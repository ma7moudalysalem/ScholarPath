using NSubstitute;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview;

namespace ScholarPath.UnitTests.CompanyReviews;

public class RefundCompanyReviewCommandHandlerTests
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly INotificationDispatcher _notifications = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<RefundCompanyReviewCommandHandler> _logger = Substitute.For<ILogger<RefundCompanyReviewCommandHandler>>();
    private readonly RefundCompanyReviewCommandHandler _handler;

    public RefundCompanyReviewCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new RefundCompanyReviewCommandHandler(
            _db,
            _stripe,
            _notifications,
            _logger);
    }

    [Fact]
    public async Task Handle_FullRefund_CancelsPaymentIntent()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var payment = new CompanyReviewPayment
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = appId,
            Status = PaymentStatus.Held,
            StripePaymentIntentId = "pi_123",
            AmountUsd = 100m
        };

        _db.CompanyReviewPayments.Add(payment);
        await _db.SaveChangesAsync();

        var command = new RefundCompanyReviewCommand(appId, IsFullRefund: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountUsd.Should().Be(100m);
        await _stripe.Received(1).CancelPaymentIntentAsync("pi_123", "requested_by_customer", Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_PartialRefund_CapturesHalfAndReleasesRest()
    {
        // Arrange
        var appId = Guid.NewGuid();
        var payment = new CompanyReviewPayment
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = appId,
            Status = PaymentStatus.Held,
            StripePaymentIntentId = "pi_123",
            AmountUsd = 100m
        };

        _db.CompanyReviewPayments.Add(payment);
        await _db.SaveChangesAsync();

        _stripe.CapturePaymentIntentAsync("pi_123", 5000, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_123", "succeeded", null, null));

        var command = new RefundCompanyReviewCommand(appId, IsFullRefund: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmountUsd.Should().Be(50m);
        await _stripe.Received(1).CapturePaymentIntentAsync("pi_123", 5000, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
