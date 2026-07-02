using Microsoft.Extensions.Logging;
using NSubstitute;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.SubmitScholarshipProviderRating;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.ScholarshipProviderReviews;

public sealed class SubmitScholarshipProviderRatingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly INotificationDispatcher _notif = Substitute.For<INotificationDispatcher>();
    private readonly ILogger<SubmitScholarshipProviderRatingCommandHandler> _logger = Substitute.For<ILogger<SubmitScholarshipProviderRatingCommandHandler>>();
    private readonly SubmitScholarshipProviderRatingCommandHandler _handler;

    public SubmitScholarshipProviderRatingCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _handler = new SubmitScholarshipProviderRatingCommandHandler(_db, _currentUser, _notif, _logger);
    }

    // Seeds a scholarship (owned by a company) + an application for the student.
    // Also seeds the company's UserProfile so the recalc/low-rating path under
    // test can write the snapshot back; tests that need to assert the
    // "profile-missing" warning path can pass seedScholarshipProviderProfile: false.
    private async Task<(Guid appId, Guid companyId)> SeedApplicationAsync(
        Guid studentId,
        ApplicationStatus status,
        bool seedScholarshipProviderProfile = true)
    {
        var companyId = Guid.NewGuid();
        var scholarship = new Scholarship
        {
            Id = Guid.NewGuid(),
            OwnerScholarshipProviderId = companyId,
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
        if (seedScholarshipProviderProfile)
        {
            _db.UserProfiles.Add(new UserProfile { UserId = companyId });
        }
        await _db.SaveChangesAsync();
        return (app.Id, companyId);
    }

    private async Task SeedExistingReviewAsync(
        Guid companyId, int rating, bool hidden = false)
    {
        // Seeds a ScholarshipProviderReview attributed to an arbitrary (unique) prior
        // application so the unique index on ApplicationTrackerId doesn't bite.
        var priorApp = new ApplicationTracker
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            Status = ApplicationStatus.Accepted,
        };
        _db.Applications.Add(priorApp);
        _db.ScholarshipProviderReviews.Add(new ScholarshipProviderReview
        {
            Id = Guid.NewGuid(),
            ApplicationTrackerId = priorApp.Id,
            StudentId = priorApp.StudentId,
            ScholarshipProviderId = companyId,
            Rating = rating,
            IsHiddenByAdmin = hidden,
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedAdminAsync()
    {
        _db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "An",
            LastName = "Admin",
            Email = $"admin-{Guid.NewGuid():N}@example.com",
            UserName = $"admin-{Guid.NewGuid():N}@example.com",
            ActiveRole = "Admin",
            AccountStatus = AccountStatus.Active,
        });
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesReview()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        var command = new SubmitScholarshipProviderRatingCommand(appId, 5, "Great experience!");

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Should().NotBeEmpty();
        var review = await _db.ScholarshipProviderReviews.FirstOrDefaultAsync(r => r.Id == result);
        review.Should().NotBeNull();
        review!.Rating.Should().Be(5);
        review.Comment.Should().Be("Great experience!");
        review.ScholarshipProviderId.Should().Be(companyId);

        await _notif.Received(1).DispatchAsync(
            companyId,
            NotificationType.ScholarshipProviderRatingReceived,
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

        var command = new SubmitScholarshipProviderRatingCommand(appId, 5, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AlreadyReviewed_ThrowsConflict()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        _db.ScholarshipProviderReviews.Add(new ScholarshipProviderReview
        {
            ApplicationTrackerId = appId,
            StudentId = studentId,
            ScholarshipProviderId = companyId,
            Rating = 4,
        });
        await _db.SaveChangesAsync();

        var command = new SubmitScholarshipProviderRatingCommand(appId, 5, null);

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    // ── Final-state gating ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AcceptedApplication_CreatesReview()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, _) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        var result = await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_RejectedApplication_CreatesReview()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, _) = await SeedApplicationAsync(studentId, ApplicationStatus.Rejected);

        var result = await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 3, null), CancellationToken.None);

        result.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(ApplicationStatus.Draft)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.Withdrawn)]
    public async Task Handle_NonFinalStatus_ThrowsConflict(ApplicationStatus status)
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, _) = await SeedApplicationAsync(studentId, status);

        await _handler.Awaiting(h => h.Handle(
                new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Handle_AnotherStudentsApplication_ThrowsForbidden()
    {
        var ownerStudent = Guid.NewGuid();
        var (appId, _) = await SeedApplicationAsync(ownerStudent, ApplicationStatus.Accepted);
        // Different student is signed in.
        _currentUser.UserId.Returns(Guid.NewGuid());

        await _handler.Awaiting(h => h.Handle(
                new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None))
            .Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── ScholarshipProviderId resolution ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ScholarshipProviderId_IsResolvedFromScholarshipOwner_NotClientSupplied()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        // The command shape has NO ScholarshipProviderId field — proving that fact at
        // compile time also satisfies the "server-side resolution" rule.
        // We additionally assert the persisted review carries the scholarship
        // owner's id, even though that id is not in the request body anywhere.
        var reviewId = await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None);

        var review = await _db.ScholarshipProviderReviews.FirstAsync(r => r.Id == reviewId);
        review.ScholarshipProviderId.Should().Be(companyId);
    }

    // ── Snapshot recalculation ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AfterSubmission_RecalculatesAverageAndCount()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        await SeedExistingReviewAsync(companyId, rating: 2);
        await SeedExistingReviewAsync(companyId, rating: 5);

        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None);

        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        profile.ScholarshipProviderReviewCount.Should().Be(3);
        // (2 + 5 + 5) / 3 = 4.00
        profile.ScholarshipProviderAverageRating.Should().Be(4.00m);
    }

    [Fact]
    public async Task Handle_AggregateExcludesHiddenReviews()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        // A 1-star hidden review must not pull the average down.
        await SeedExistingReviewAsync(companyId, rating: 1, hidden: true);

        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 5, null), CancellationToken.None);

        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        profile.ScholarshipProviderReviewCount.Should().Be(1);
        profile.ScholarshipProviderAverageRating.Should().Be(5.00m);
    }

    // ── Low-rating policy ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_AverageBelowThreshold_FlagsScholarshipProviderForAdminReview()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);
        await SeedAdminAsync();

        // Single 1-star drops avg to 1.00 (< 2.5).
        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 1, "Did not deliver"), CancellationToken.None);

        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        profile.ScholarshipProviderLowRatingFlaggedAt.Should().NotBeNull();
        profile.ScholarshipProviderAverageRating.Should().Be(1.00m);

        await _notif.Received().DispatchAsync(
            Arg.Any<Guid>(),
            NotificationType.ScholarshipProviderLowRatingFlagged,
            Arg.Any<NotificationParams>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AverageAtOrAboveThreshold_DoesNotFlag()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);
        await SeedAdminAsync();

        // Exactly 2.5 sits ON the boundary — spec says "below 2.5" so this
        // must NOT flag. Seed a 2 + submit a 3 → avg 2.50.
        await SeedExistingReviewAsync(companyId, rating: 2);
        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 3, null), CancellationToken.None);

        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        profile.ScholarshipProviderLowRatingFlaggedAt.Should().BeNull();
        profile.ScholarshipProviderAverageRating.Should().Be(2.50m);

        await _notif.DidNotReceive().DispatchAsync(
            Arg.Any<Guid>(),
            NotificationType.ScholarshipProviderLowRatingFlagged,
            Arg.Any<NotificationParams>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyFlagged_DoesNotOverwriteFlaggedAt()
    {
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);
        await SeedAdminAsync();

        // Pre-flag this company a week ago.
        var profile = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        var originalFlaggedAt = DateTimeOffset.UtcNow.AddDays(-7);
        profile.ScholarshipProviderLowRatingFlaggedAt = originalFlaggedAt;
        await _db.SaveChangesAsync();

        // Another sub-2.5 rating arrives.
        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 1, null), CancellationToken.None);

        var updated = await _db.UserProfiles.SingleAsync(p => p.UserId == companyId);
        // Sticky: original timestamp preserved.
        updated.ScholarshipProviderLowRatingFlaggedAt.Should().Be(originalFlaggedAt);
        // And we should NOT re-notify admins — the "firstFlagging" gate kept
        // dispatch from firing a second time.
        await _notif.DidNotReceive().DispatchAsync(
            Arg.Any<Guid>(),
            NotificationType.ScholarshipProviderLowRatingFlagged,
            Arg.Any<NotificationParams>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StillDispatchesScholarshipProviderRatingReceived_RegardlessOfThreshold()
    {
        // Regression guard: PB-005R must not break the existing per-company
        // "you received a rating" notification.
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, companyId) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 1, null), CancellationToken.None);

        await _notif.Received(1).DispatchAsync(
            companyId,
            NotificationType.ScholarshipProviderRatingReceived,
            Arg.Any<NotificationParams>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DoesNotTouchSavedScholarships()
    {
        // PB-005 bookmark-independence rule extends to rating: submitting a
        // rating must not read or write SavedScholarships.
        var studentId = Guid.NewGuid();
        _currentUser.UserId.Returns(studentId);
        var (appId, _) = await SeedApplicationAsync(studentId, ApplicationStatus.Accepted);

        var bookmark = new SavedScholarship
        {
            Id = Guid.NewGuid(),
            UserId = studentId,
            ScholarshipId = (await _db.Applications.FindAsync(appId))!.ScholarshipId!.Value,
            SavedAt = DateTimeOffset.UtcNow.AddDays(-1),
        };
        _db.SavedScholarships.Add(bookmark);
        await _db.SaveChangesAsync();

        await _handler.Handle(
            new SubmitScholarshipProviderRatingCommand(appId, 4, null), CancellationToken.None);

        var stillThere = await _db.SavedScholarships.FindAsync(bookmark.Id);
        stillThere.Should().NotBeNull();
    }

    public void Dispose() => _db.Dispose();
}
