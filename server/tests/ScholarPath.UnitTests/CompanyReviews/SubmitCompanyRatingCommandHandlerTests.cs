using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.CompanyReviews;

public class SubmitCompanyRatingCommandHandlerTests
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly INotificationDispatcher _notif = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<SubmitCompanyRatingCommandHandler> _logger = Substitute.For<ILogger<SubmitCompanyRatingCommandHandler>>();
    private readonly SubmitCompanyRatingCommandHandler _handler;

    public SubmitCompanyRatingCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new SubmitCompanyRatingCommandHandler(
            _db,
            _currentUser,
            _notif,
            _logger);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesReview()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUser.UserId.Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.Accepted,
            ScholarshipId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, companyId, 5, "Great experience!");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        var review = await _db.CompanyReviews.FirstOrDefaultAsync(r => r.Id == result);
        review.Should().NotBeNull();
        review!.Rating.Should().Be(5);
        review.Comment.Should().Be("Great experience!");
        
        await _notif.Received(1).DispatchAsync(
            companyId,
            NotificationType.CompanyRatingReceived,
            Arg.Any<NotificationContent>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFinal_ThrowsConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUser.UserId.Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.UnderReview,
            ScholarshipId = Guid.NewGuid()
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, Guid.NewGuid(), 5, null);

        // Act & Assert
        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflict()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var appId = Guid.NewGuid();

        _currentUser.UserId.Returns(studentId);

        _db.Applications.Add(new ApplicationTracker
        {
            Id = appId,
            StudentId = studentId,
            Status = ApplicationStatus.Accepted,
            ScholarshipId = Guid.NewGuid()
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
        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }
}
