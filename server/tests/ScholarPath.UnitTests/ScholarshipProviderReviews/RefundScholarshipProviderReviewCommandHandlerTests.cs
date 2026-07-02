using NSubstitute;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.RefundScholarshipProviderReview;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

/// <summary>
/// Verifies PB-005 v1 ScholarshipProviderReview refund behaviour against the unified
/// <see cref="Payment"/> table (the legacy <c>ScholarshipProviderReviewPayment</c> table
/// is no longer used for the active flow).
/// </summary>
public sealed class RefundScholarshipProviderReviewCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<RefundScholarshipProviderReviewCommandHandler> _logger =
        Substitute.For<ILogger<RefundScholarshipProviderReviewCommandHandler>>();
    private readonly RefundScholarshipProviderReviewCommandHandler _handler;

    public RefundScholarshipProviderReviewCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new RefundScholarshipProviderReviewCommandHandler(
            _db, _stripe, _notifications, _logger);
    }

    public void Dispose() => _db.Dispose();

    private Payment SeedHeldPayment(Guid applicationId, long amountCents = 10_000)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Held,
            AmountCents = amountCents,
            Currency = "USD",
            ProfitShareAmountCents = amountCents / 10,
            PayeeAmountCents = amountCents - amountCents / 10,
            RefundedAmountCents = 0,
            PayerUserId = Guid.NewGuid(),
            PayeeUserId = Guid.NewGuid(),
            StripePaymentIntentId = $"pi_{Guid.NewGuid():N}",
            IdempotencyKey = $"key_{Guid.NewGuid():N}",
            RelatedApplicationId = applicationId,
        };
        _db.Payments.Add(payment);
        return payment;
    }

    private Payment SeedCapturedPayment(Guid applicationId, long amountCents = 10_000)
    {
        var payment = SeedHeldPayment(applicationId, amountCents);
        payment.Status = PaymentStatus.Captured;
        payment.CapturedAt = DateTimeOffset.UtcNow;
        return payment;
    }

    [Fact]
    public async Task Full_refund_on_Held_payment_cancels_intent_and_marks_Cancelled()
    {
        var appId = Guid.NewGuid();
        var payment = SeedHeldPayment(appId);
        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId, StudentId = Guid.NewGuid(), Status = ApplicationStatus.Pending,
        });
        await _db.SaveChangesAsync();

        _stripe.CancelPaymentIntentAsync(
                payment.StripePaymentIntentId!, Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "canceled", null, null));

        var result = await _handler.Handle(
            new RefundScholarshipProviderReviewCommand(appId, IsFullRefund: true), default);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.RefundedAmountCents.Should().Be(0);
        payment.ProfitShareAmountCents.Should().Be(0);
        payment.PayeeAmountCents.Should().Be(0);
        await _stripe.Received(1).CancelPaymentIntentAsync(
            payment.StripePaymentIntentId!, Arg.Any<string?>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _stripe.DidNotReceiveWithAnyArgs()
            .RefundPaymentAsync(default!, default, default, default!, default);
    }

    [Fact]
    public async Task Full_refund_on_Captured_payment_issues_full_Stripe_refund()
    {
        var appId = Guid.NewGuid();
        var payment = SeedCapturedPayment(appId, 10_000);
        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId, StudentId = Guid.NewGuid(), Status = ApplicationStatus.UnderReview,
        });
        await _db.SaveChangesAsync();

        _stripe.RefundPaymentAsync(
                payment.StripePaymentIntentId!, 10_000, Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_full", "succeeded", 10_000));

        var result = await _handler.Handle(
            new RefundScholarshipProviderReviewCommand(appId, IsFullRefund: true), default);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountCents.Should().Be(10_000);
        payment.ProfitShareAmountCents.Should().Be(0);
        payment.PayeeAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Partial_refund_on_Captured_payment_refunds_50_percent_and_recomputes_split()
    {
        var appId = Guid.NewGuid();
        var payment = SeedCapturedPayment(appId, 10_000);
        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId, StudentId = Guid.NewGuid(), Status = ApplicationStatus.UnderReview,
        });
        await _db.SaveChangesAsync();

        _stripe.RefundPaymentAsync(
                payment.StripePaymentIntentId!, 5_000, Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_half", "succeeded", 5_000));

        var result = await _handler.Handle(
            new RefundScholarshipProviderReviewCommand(appId, IsFullRefund: false), default);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmountCents.Should().Be(5_000);
        // Retained = 5000; default v1 split = 10% / 90%.
        payment.ProfitShareAmountCents.Should().Be(500);
        payment.PayeeAmountCents.Should().Be(4_500);
    }

    [Fact]
    public async Task Returns_false_when_no_companyreview_payment_exists()
    {
        var result = await _handler.Handle(
            new RefundScholarshipProviderReviewCommand(Guid.NewGuid(), IsFullRefund: true), default);

        result.Should().BeFalse();
        await _stripe.DidNotReceiveWithAnyArgs()
            .CancelPaymentIntentAsync(default!, default, default!, default);
        await _stripe.DidNotReceiveWithAnyArgs()
            .RefundPaymentAsync(default!, default, default, default!, default);
    }

    [Fact]
    public async Task Does_not_touch_unrelated_ConsultantBooking_payments()
    {
        var appId = Guid.NewGuid();
        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Held,
            AmountCents = 10_000,
            Currency = "USD",
            ProfitShareAmountCents = 1_000,
            PayeeAmountCents = 9_000,
            PayerUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_booking",
            IdempotencyKey = "k_booking",
            RelatedApplicationId = appId,
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(
            new RefundScholarshipProviderReviewCommand(appId, IsFullRefund: true), default);

        result.Should().BeFalse();
        await _stripe.DidNotReceiveWithAnyArgs()
            .CancelPaymentIntentAsync(default!, default, default!, default);
    }
}
