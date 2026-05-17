using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.CompanyReviews;

public sealed class SubmitCompanyRatingCommandHandlerTests : IDisposable
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

        _handler = new SubmitCompanyRatingCommandHandler(_db, _currentUser, _notif, _logger);
    }

    // Seeds a scholarship (owned by a company) + an application for the student.
    private async Task<(Guid appId, Guid companyId)> SeedApplicationAsync(
        Guid studentId, ApplicationStatus status)
    {
        var companyId = Guid.NewGuid();
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerCompanyId = companyId,
            TitleEn = "Test Scholarship",
            TitleAr = "منحة اختبار",
            DescriptionEn = "Description",
            DescriptionAr = "وصف",
            Slug = $"test-scholarship-{Guid.NewGuid():N}",
            CategoryId = Guid.NewGuid(),
            Deadline = DateTimeOffset.UtcNow.AddDays(30),
        };
        var app = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Status = status,
            ScholarshipId = scholarship.Id,
        };
        _db.Scholarships.Add(scholarship);
        _db.Applications.Add(app);
        await _db.SaveChangesAsync();
        return (app.Id, companyId);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesReview()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        var command = new SubmitCompanyRatingCommand(appId, 5, "Great experience!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        var review = await _db.CompanyReviews.FirstOrDefaultAsync(r => r.Id == result);
        review.Should().NotBeNull();
        review!.Rating.Should().Be(5);
        review.Comment.Should().Be("Great experience!");
        review.CompanyId.Should().Be(companyId);

        await _notif.Received(1).DispatchAsync(
            companyId,
            NotificationType.CompanyRatingReceived,
            Arg.Any<NotificationParams>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ApplicationNotFinal_ThrowsConflict()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, _) = await SeedApplicationAsync(studentId, ApplicationStatus.UnderReview);

        var command = new SubmitCompanyRatingCommand(appId, 5, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflict()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        _db.CompanyReviews.Add(new CompanyReview
        {
            ApplicationTrackerId = appId,
            StudentId = studentId,
            CompanyId = companyId,
            Rating = 4,
        });
        await _db.SaveChangesAsync();

        var command = new SubmitCompanyRatingCommand(appId, 5, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    public void Dispose() => _db.Dispose();
}
