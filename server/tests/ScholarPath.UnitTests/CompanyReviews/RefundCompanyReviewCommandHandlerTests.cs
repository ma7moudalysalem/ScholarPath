using Moq;
using Moq.EntityFrameworkCore;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview;

namespace ScholarPath.UnitTests.CompanyReviews;

public class RefundCompanyReviewCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbMock;
    private readonly Mock<IStripeService> _stripeMock;
    private readonly Mock<INotificationDispatcher> _notificationsMock;
    private readonly Mock<ILogger<RefundCompanyReviewCommandHandler>> _loggerMock;
    private readonly RefundCompanyReviewCommandHandler _handler;

    public RefundCompanyReviewCommandHandlerTests()
    {
        _dbMock = new Mock<IApplicationDbContext>();
        _stripeMock = new Mock<IStripeService>();
        _notificationsMock = new Mock<INotificationDispatcher>();
        _loggerMock = new Mock<ILogger<RefundCompanyReviewCommandHandler>>();

        _handler = new RefundCompanyReviewCommandHandler(
            _dbMock.Object,
            _stripeMock.Object,
            _notificationsMock.Object,
            _loggerMock.Object);
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

        _dbMock.Setup(db => db.CompanyReviewPayments)
            .ReturnsDbSet(new List<CompanyReviewPayment> { payment });
            
        _dbMock.Setup(db => db.Applications)
            .ReturnsDbSet(new List<ApplicationTracker>());

        var command = new RefundCompanyReviewCommand(appId, IsFullRefund: true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.Refunded);
        payment.RefundedAmountUsd.Should().Be(100m);
        _stripeMock.Verify(s => s.CancelPaymentIntentAsync("pi_123", "requested_by_customer", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _dbMock.Setup(db => db.CompanyReviewPayments)
            .ReturnsDbSet(new List<CompanyReviewPayment> { payment });
            
        _dbMock.Setup(db => db.Applications)
            .ReturnsDbSet(new List<ApplicationTracker>());

        _stripeMock.Setup(s => s.CapturePaymentIntentAsync("pi_123", 5000, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ScholarPath.Application.Common.Interfaces.StripePaymentIntentResponse("succeeded", "pi_123", 5000));

        var command = new RefundCompanyReviewCommand(appId, IsFullRefund: false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmountUsd.Should().Be(50m);
        _stripeMock.Verify(s => s.CapturePaymentIntentAsync("pi_123", 5000, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
