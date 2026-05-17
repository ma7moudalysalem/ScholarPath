using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Notifications;

/// <summary>
/// FR-046 / FR-062 — the deadline-reminder and draft-reminder sweep jobs.
/// Mirrors StripePayoutJobTests: InMemory EF + an NSubstitute dispatcher.
/// </summary>
public class ReminderJobsTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static Scholarship Listing(
        ScholarshipStatus status, DateTimeOffset deadline, string title = "Fund") => new()
    {
        Id = Guid.NewGuid(),
        TitleEn = title,
        TitleAr = title + " AR",
        Slug = "fund-" + Guid.NewGuid().ToString("N")[..8],
        DescriptionEn = "",
        DescriptionAr = "",
        Status = status,
        FundingType = FundingType.FullyFunded,
        TargetLevel = AcademicLevel.Masters,
        Mode = ListingMode.InApp,
        Deadline = deadline,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    private static ApplicationTracker Application(
        Guid studentId, Guid scholarshipId, ApplicationStatus status,
        ApplicationMode mode = ApplicationMode.InApp) => new()
    {
        Id = Guid.NewGuid(),
        StudentId = studentId,
        ScholarshipId = scholarshipId,
        Mode = mode,
        Status = status,
        CreatedAt = DateTimeOffset.UtcNow,
    };

    // ─── DeadlineReminderJob (FR-046) ────────────────────────────────────────

    [Fact]
    public async Task Deadline_job_reminds_a_student_who_bookmarked_a_listing_closing_soon()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(3));
        db.Scholarships.Add(listing);
        db.SavedScholarships.Add(new SavedScholarship { Id = Guid.NewGuid(), UserId = studentId, ScholarshipId = listing.Id });
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new DeadlineReminderJob(db, notifier, NullLogger<DeadlineReminderJob>.Instance)
            .RunAsync(default);

        await notifier.Received(1).DispatchAsync(
            studentId,
            NotificationType.ApplicationDeadlineApproaching,
            Arg.Any<NotificationParams>(),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k != null && k.StartsWith("deadline-reminder:")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deadline_job_reminds_a_student_with_a_live_application()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(2));
        db.Scholarships.Add(listing);
        db.Applications.Add(Application(studentId, listing.Id, ApplicationStatus.UnderReview));
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new DeadlineReminderJob(db, notifier, NullLogger<DeadlineReminderJob>.Instance)
            .RunAsync(default);

        await notifier.Received(1).DispatchAsync(
            studentId, NotificationType.ApplicationDeadlineApproaching,
            Arg.Any<NotificationParams>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deadline_job_ignores_listings_outside_the_window_or_not_open()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();

        var farOff = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(30));
        var alreadyPassed = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(-1));
        var closedSoon = Listing(ScholarshipStatus.Closed, DateTimeOffset.UtcNow.AddDays(2));
        db.Scholarships.AddRange(farOff, alreadyPassed, closedSoon);
        db.SavedScholarships.AddRange(
            new SavedScholarship { Id = Guid.NewGuid(), UserId = studentId, ScholarshipId = farOff.Id },
            new SavedScholarship { Id = Guid.NewGuid(), UserId = studentId, ScholarshipId = alreadyPassed.Id },
            new SavedScholarship { Id = Guid.NewGuid(), UserId = studentId, ScholarshipId = closedSoon.Id });
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new DeadlineReminderJob(db, notifier, NullLogger<DeadlineReminderJob>.Instance)
            .RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    [Fact]
    public async Task Deadline_job_skips_students_with_a_terminal_application()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(3));
        db.Scholarships.Add(listing);
        // The student already has an accepted application — no reminder needed,
        // even though a terminal application leaves no bookmark behind here.
        db.Applications.Add(Application(studentId, listing.Id, ApplicationStatus.Accepted));
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new DeadlineReminderJob(db, notifier, NullLogger<DeadlineReminderJob>.Instance)
            .RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    [Fact]
    public async Task Deadline_job_uses_one_stable_idempotency_key_per_scholarship_recipient_deadline()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(4));
        db.Scholarships.Add(listing);
        // Bookmarked AND has a live application — the recipient is de-duplicated,
        // so exactly one reminder fires with one key.
        db.SavedScholarships.Add(new SavedScholarship { Id = Guid.NewGuid(), UserId = studentId, ScholarshipId = listing.Id });
        db.Applications.Add(Application(studentId, listing.Id, ApplicationStatus.Pending));
        await db.SaveChangesAsync();

        var keys = new List<string?>();
        var notifier = Substitute.For<INotificationDispatcher>();
        await notifier.DispatchAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
            Arg.Any<string?>(), Arg.Do<string?>(k => keys.Add(k)), Arg.Any<CancellationToken>());

        await new DeadlineReminderJob(db, notifier, NullLogger<DeadlineReminderJob>.Instance)
            .RunAsync(default);

        keys.Should().ContainSingle();
        keys[0].Should().Contain(listing.Id.ToString("N")).And.Contain(studentId.ToString("N"));
    }

    // ─── NotificationDispatcherJob — draft reminders (FR-062) ────────────────

    [Fact]
    public async Task Draft_job_reminds_a_student_with_an_open_unsubmitted_draft()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(10));
        db.Scholarships.Add(listing);
        db.Applications.Add(Application(studentId, listing.Id, ApplicationStatus.Draft));
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new NotificationDispatcherJob(db, notifier, NullLogger<NotificationDispatcherJob>.Instance)
            .RunAsync(default);

        await notifier.Received(1).DispatchAsync(
            studentId,
            NotificationType.ApplicationDraftReminder,
            Arg.Any<NotificationParams>(),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k != null && k.StartsWith("draft-reminder:")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Draft_job_ignores_submitted_applications_and_closed_or_expired_listings()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();

        var openListing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(10));
        var closedListing = Listing(ScholarshipStatus.Closed, DateTimeOffset.UtcNow.AddDays(10));
        var expiredListing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(-2));
        db.Scholarships.AddRange(openListing, closedListing, expiredListing);

        // Submitted (not Draft) on an open listing — no nudge.
        db.Applications.Add(Application(studentId, openListing.Id, ApplicationStatus.Pending));
        // Draft, but the listing is closed — no nudge.
        db.Applications.Add(Application(studentId, closedListing.Id, ApplicationStatus.Draft));
        // Draft, but the deadline has passed — no nudge.
        db.Applications.Add(Application(studentId, expiredListing.Id, ApplicationStatus.Draft));
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        await new NotificationDispatcherJob(db, notifier, NullLogger<NotificationDispatcherJob>.Instance)
            .RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    [Fact]
    public async Task Draft_job_keys_idempotency_to_the_draft_application()
    {
        using var db = CreateDb();
        var studentId = Guid.NewGuid();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(15));
        db.Scholarships.Add(listing);
        var draft = Application(studentId, listing.Id, ApplicationStatus.Draft);
        db.Applications.Add(draft);
        await db.SaveChangesAsync();

        var keys = new List<string?>();
        var notifier = Substitute.For<INotificationDispatcher>();
        await notifier.DispatchAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
            Arg.Any<string?>(), Arg.Do<string?>(k => keys.Add(k)), Arg.Any<CancellationToken>());

        await new NotificationDispatcherJob(db, notifier, NullLogger<NotificationDispatcherJob>.Instance)
            .RunAsync(default);

        keys.Should().ContainSingle();
        keys[0].Should().Contain(draft.Id.ToString("N"));
    }

    [Fact]
    public async Task Draft_job_continues_after_a_dispatch_failure()
    {
        using var db = CreateDb();
        var listing = Listing(ScholarshipStatus.Open, DateTimeOffset.UtcNow.AddDays(10));
        db.Scholarships.Add(listing);
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        db.Applications.Add(Application(studentA, listing.Id, ApplicationStatus.Draft));
        db.Applications.Add(Application(studentB, listing.Id, ApplicationStatus.Draft));
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        // First student's dispatch throws; the job must still reach the second.
        notifier.DispatchAsync(
                studentA, Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
                Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("smtp down"));

        var act = () => new NotificationDispatcherJob(db, notifier, NullLogger<NotificationDispatcherJob>.Instance)
            .RunAsync(default);

        await act.Should().NotThrowAsync();
        await notifier.Received(1).DispatchAsync(
            studentB, Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
            Arg.Any<string?>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }
}
