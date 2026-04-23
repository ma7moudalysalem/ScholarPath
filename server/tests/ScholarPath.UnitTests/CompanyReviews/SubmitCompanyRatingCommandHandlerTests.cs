using Microsoft.Extensions.Logging;
using Moq;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.UnitTests.Common;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviews;

public class SubmitCompanyRatingCommandHandlerTests
{
    private readonly TestDbContext _db;
    private readonly Mock<ICurrentUserService> _currentUserMock;
    private readonly Mock<INotificationDispatcher> _notifMock;
    private readonly Mock<ILogger<SubmitCompanyRatingCommandHandler>> _loggerMock;
    private readonly SubmitCompanyRatingCommandHandler _handler;

    public SubmitCompanyRatingCommandHandlerTests()
    {
        _db = TestDbContextFactory.Create();
        _currentUserMock = new Mock<ICurrentUserService>();
        _notifMock = new Mock<INotificationDispatcher>();
        _loggerMock = new Mock<ILogger<SubmitCompanyRatingCommandHandler>>();

        _handler = new SubmitCompanyRatingCommandHandler(
            _db,
            _currentUserMock.Object,
            _notifMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesReview()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUserMock.Setup(x => x.UserId).Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.Accepted
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, companyId, 5, "Great experience!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, result);
        var review = _db.CompanyReviews.FirstOrDefault(r => r.Id == result);
        Assert.NotNull(review);
        Assert.Equal(5, review.Rating);
        Assert.Equal("Great experience!", review.Comment);
        
        _notifMock.Verify(x => x.DispatchAsync(
            companyId,
            NotificationType.CompanyRatingReceived,
            It.IsAny<NotificationContent>(),
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ApplicationNotFinal_ThrowsConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUserMock.Setup(x => x.UserId).Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.UnderReview
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, Guid.NewGuid(), 5, null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUserMock.Setup(x => x.UserId).Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.Accepted
        });
        _db.CompanyReviews.Add(new CompanyReview
        {
            ApplicationTrackerId = appId,
            StudentId = studentId,
            CompanyId = Guid.NewGuid(),
            Rating = 4
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, Guid.NewGuid(), 5, null);

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(command, CancellationToken.None));
    }
}
