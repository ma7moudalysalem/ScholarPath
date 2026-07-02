using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

/// <summary>
/// PB-005 v1: the timeout job refunds ScholarshipProviderReview payments on the unified
/// <see cref="Payment"/> table when the company misses the 14-day review
/// window after the scholarship deadline.
/// </summary>
public sealed class ScholarshipProviderReviewTimeoutRefundJobTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ScholarshipProviderReviewTimeoutRefundJob _job;

    public ScholarshipProviderReviewTimeoutRefundJobTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _job = new ScholarshipProviderReviewTimeoutRefundJob(
            _db, _stripe, _notifications,
            NullLogger<ScholarshipProviderReviewTimeoutRefundJob>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private (ApplicationTracker app, Payment payment) SeedExpired(
        PaymentStatus status,
        long amountCents = 10_000)
    {
        var scholarshipId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _db.Scholarships.Add(new Scholarship
        {
            Id = scholarshipId,
            TitleEn = "S", TitleAr = "س",
            Slug = $"s-{Guid.NewGuid():N}",
            DescriptionEn = "d", DescriptionAr = "د",
            Deadline = DateTimeOffset.UtcNow.AddDays(-30),
            OwnerScholarshipProviderId = Guid.NewGuid(),
        });
        var app = new ApplicationTracker
        {
            Id = appId,
            StudentId = Guid.NewGuid(),
            ScholarshipId = scholarshipId,
            Status = ApplicationStatus.UnderReview,
        };
        _db.Applications.Add(app);
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = status,
            AmountCents = amountCents,
            ProfitShareAmountCents = amountCents / 10,
            PayeeAmountCents = amountCents - amountCents / 10,
            Currency = "USD",
            PayerUserId = app.StudentId,
            PayeeUserId = Guid.NewGuid(),
            StripePaymentIntentId = $"pi_{Guid.NewGuid():N}",
            IdempotencyKey = $"k_{Guid.NewGuid():N}",
            RelatedApplicationId = appId,
            CapturedAt = status == PaymentStatus.Captured ? DateTimeOffset.UtcNow.AddDays(-20) : null,
        };
        _db.Payments.Add(payment);
        return (app, payment);
    }

    [Fact]
    public async Task Cancels_held_PaymentIntent_for_expired_application()
    {
        var (_, payment) = SeedExpired(PaymentStatus.Held);
        await _db.SaveChangesAsync();

        _stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId!, Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "canceled", null, null));

        await _job.RunAsync(default);

        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.ProfitShareAmountCents.Should().Be(0);
        payment.PayeeAmountCents.Should().Be(0);
        await _stripe.DidNotReceiveWithAnyArgs()
            .RefundPaymentAsync(default!, default, default, default!, default);
    }

    [Fact]
    public async Task Refunds_captured_payment_in_full_after_timeout()
    {
        var (_, payment) = SeedExpired(PaymentStatus.Captured, 10_000);
        await _db.SaveChangesAsync();

        _stripe.RefundPaymentAsync(
                payment.StripePaymentIntentId!, 10_000, Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_to", "succeeded", 10_000));

        await _job.RunAsync(default);

        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountCents.Should().Be(10_000);
        payment.ProfitShareAmountCents.Should().Be(0);
        payment.PayeeAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Does_not_touch_applications_whose_deadline_is_in_the_window()
    {
        var scholarshipId = Guid.NewGuid();
        var appId = Guid.NewGuid();
        _db.Scholarships.Add(new Scholarship
        {
            Id = scholarshipId,
            TitleEn = "S", TitleAr = "س",
            Slug = $"s-{Guid.NewGuid():N}",
            DescriptionEn = "d", DescriptionAr = "د",
            Deadline = DateTimeOffset.UtcNow.AddDays(-5), // < 14 days ago
        });
        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = Guid.NewGuid(),
            ScholarshipId = scholarshipId,
            Status = ApplicationStatus.UnderReview,
        });
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Held,
            AmountCents = 5_000,
            Currency = "USD",
            PayerUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_fresh",
            IdempotencyKey = "k_fresh",
            RelatedApplicationId = appId,
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        await _job.RunAsync(default);

        payment.Status.Should().Be(PaymentStatus.Held);
        await _stripe.DidNotReceiveWithAnyArgs()
            .CancelPaymentIntentAsync(default!, default, default!, default);
    }
}
