using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviews.Commands.RejectCompanyReviewPayment;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviews;

/// <summary>
/// PB-005 v1: a company rejection on an application with a held review fee
/// must cancel the PaymentIntent — the student is never charged.
/// </summary>
public sealed class RejectCompanyReviewPaymentCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly RejectCompanyReviewPaymentCommandHandler _handler;

    public RejectCompanyReviewPaymentCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        _handler = new RejectCompanyReviewPaymentCommandHandler(
            _db, _stripe, _notifications,
            NullLogger<RejectCompanyReviewPaymentCommandHandler>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Cancels_held_CompanyReview_PaymentIntent_on_rejection()
    {
        var appId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.CompanyReview,
            Status = PaymentStatus.Held,
            AmountCents = 10_000,
            ProfitShareAmountCents = 1_000,
            PayeeAmountCents = 9_000,
            Currency = "USD",
            PayerUserId = studentId,
            StripePaymentIntentId = "pi_reject",
            IdempotencyKey = "k_reject",
            RelatedApplicationId = appId,
        };
        _db.Payments.Add(payment);
        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId, StudentId = studentId, Status = ApplicationStatus.Rejected,
        });
        await _db.SaveChangesAsync();

        _stripe.CancelPaymentIntentAsync(
                "pi_reject", Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_reject", "canceled", null, null));

        var result = await _handler.Handle(
            new RejectCompanyReviewPaymentCommand(appId), default);

        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.ProfitShareAmountCents.Should().Be(0);
        payment.PayeeAmountCents.Should().Be(0);
        payment.RefundedAmountCents.Should().Be(0);
        payment.FailureReason.Should().Be("company_rejected_application");
        await _stripe.DidNotReceiveWithAnyArgs()
            .CapturePaymentIntentAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Returns_false_when_no_held_payment_exists()
    {
        var result = await _handler.Handle(
            new RejectCompanyReviewPaymentCommand(Guid.NewGuid()), default);

        result.Should().BeFalse();
        await _stripe.DidNotReceiveWithAnyArgs()
            .CancelPaymentIntentAsync(default!, default, default!, default);
    }
}
