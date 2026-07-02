using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.CaptureScholarshipProviderReviewPayment;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

/// <summary>
/// PB-005 v1: ScholarshipProviderReview capture must operate on the unified <see cref="Payment"/>
/// table and reset the platform/payee split using the rule in force at capture time.
/// </summary>
public sealed class CaptureScholarshipProviderReviewPaymentCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly CaptureScholarshipProviderReviewPaymentCommandHandler _handler;

    public CaptureScholarshipProviderReviewPaymentCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        _handler = new CaptureScholarshipProviderReviewPaymentCommandHandler(
            _db, _stripe, _notifications,
            NullLogger<CaptureScholarshipProviderReviewPaymentCommandHandler>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Captures_Held_ScholarshipProviderReview_payment_and_locks_in_v1_split()
    {
        var appId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Held,
            AmountCents = 10_000,
            // Pre-capture snapshot from intent-creation — handler should overwrite.
            ProfitShareAmountCents = 0,
            PayeeAmountCents = 0,
            Currency = "USD",
            PayerUserId = Guid.NewGuid(),
            PayeeUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_test",
            IdempotencyKey = "k_test",
            RelatedApplicationId = appId,
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _stripe.CapturePaymentIntentAsync(
                "pi_test", null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_test", "succeeded", null, "ch_test"));

        var result = await _handler.Handle(new CaptureScholarshipProviderReviewPaymentCommand(appId), default);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
        payment.StripeChargeId.Should().Be("ch_test");
        // Default v1: commission = 10%, payee = 90%.
        payment.ProfitShareAmountCents.Should().Be(1_000);
        payment.PayeeAmountCents.Should().Be(9_000);
    }

    [Fact]
    public async Task Returns_false_when_no_held_ScholarshipProviderReview_payment_exists()
    {
        var result = await _handler.Handle(
            new CaptureScholarshipProviderReviewPaymentCommand(Guid.NewGuid()), default);

        result.Should().BeFalse();
        await _stripe.DidNotReceiveWithAnyArgs()
            .CapturePaymentIntentAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Does_not_capture_ConsultantBooking_payment_with_matching_id()
    {
        var appId = Guid.NewGuid();
        _db.Payments.Add(new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking, // wrong type
            Status = PaymentStatus.Held,
            AmountCents = 10_000,
            Currency = "USD",
            PayerUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_boo",
            IdempotencyKey = "k_boo",
            RelatedApplicationId = appId,
        });
        await _db.SaveChangesAsync();

        var result = await _handler.Handle(new CaptureScholarshipProviderReviewPaymentCommand(appId), default);

        result.Should().BeFalse();
        await _stripe.DidNotReceiveWithAnyArgs()
            .CapturePaymentIntentAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Leaves_payment_untouched_when_Stripe_capture_does_not_succeed()
    {
        var appId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ScholarshipProviderReview,
            Status = PaymentStatus.Held,
            AmountCents = 5_000,
            Currency = "USD",
            PayerUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_x",
            IdempotencyKey = "k_x",
            RelatedApplicationId = appId,
        };
        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        _stripe.CapturePaymentIntentAsync(
                "pi_x", null, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_x", "requires_action", null, null));

        var result = await _handler.Handle(new CaptureScholarshipProviderReviewPaymentCommand(appId), default);

        result.Should().BeFalse();
        payment.Status.Should().Be(PaymentStatus.Held);
        payment.CapturedAt.Should().BeNull();
    }
}
