using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Hubs;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// PB-006 — BookingReminderJob fires BookingReminder notifications to the student
/// and consultant 24 hours and 1 hour before a Confirmed session, idempotent per
/// (booking, kind, recipient).
/// </summary>
public sealed class BookingReminderJobTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options);

    private static ApplicationUser NewUser(Guid id, string first, string last)
    {
        var email = $"{first}@test.com";
        return new ApplicationUser
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Email = email,
            UserName = email,
            AccountStatus = AccountStatus.Active,
        };
    }

    private static (Guid StudentId, Guid ConsultantId, Guid BookingId) Seed(
        ApplicationDbContext db,
        DateTimeOffset scheduledStartAt,
        BookingStatus status = BookingStatus.Confirmed,
        string studentFirst = "Tasneem",
        string consultantFirst = "Sarah")
    {
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var bookingId = Guid.NewGuid();

        db.Users.Add(NewUser(studentId, studentFirst, "Shaban"));
        db.Users.Add(NewUser(consultantId, consultantFirst, "Adel"));
        db.Bookings.Add(new ConsultantBooking
        {
            Id = bookingId,
            StudentId = studentId,
            ConsultantId = consultantId,
            Status = status,
            ScheduledStartAt = scheduledStartAt,
            ScheduledEndAt = scheduledStartAt.AddMinutes(45),
            DurationMinutes = 45,
            PriceUsd = 100m,
        });
        db.SaveChanges();
        return (studentId, consultantId, bookingId);
    }

    private static BookingReminderJob Sut(ApplicationDbContext db, INotificationDispatcher notifier) =>
        new(db, notifier, NullLogger<BookingReminderJob>.Instance);

    // ─── 24-hour reminder ────────────────────────────────────────────────────

    [Fact]
    public async Task Sends_24h_reminder_when_session_is_inside_24h_and_more_than_1h_away()
    {
        using var db = CreateDb();
        var startAt = DateTimeOffset.UtcNow.AddHours(20);
        var ids = Seed(db, startAt);

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        // Student + Consultant, both with the "24h" kind.
        await notifier.Received(1).DispatchAsync(
            ids.StudentId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p => p.ReminderKind == "24h"),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k == $"booking-reminder:{ids.BookingId:N}:24h:student"),
            Arg.Any<CancellationToken>());
        await notifier.Received(1).DispatchAsync(
            ids.ConsultantId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p => p.ReminderKind == "24h"),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k == $"booking-reminder:{ids.BookingId:N}:24h:consultant"),
            Arg.Any<CancellationToken>());
    }

    // ─── 1-hour reminder ─────────────────────────────────────────────────────

    [Fact]
    public async Task Sends_1h_reminder_when_session_is_within_the_next_hour()
    {
        using var db = CreateDb();
        var startAt = DateTimeOffset.UtcNow.AddMinutes(45);
        var ids = Seed(db, startAt);

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        await notifier.Received(1).DispatchAsync(
            ids.StudentId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p => p.ReminderKind == "1h"),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k == $"booking-reminder:{ids.BookingId:N}:1h:student"),
            Arg.Any<CancellationToken>());
        await notifier.Received(1).DispatchAsync(
            ids.ConsultantId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p => p.ReminderKind == "1h"),
            Arg.Any<string?>(),
            Arg.Is<string?>(k => k == $"booking-reminder:{ids.BookingId:N}:1h:consultant"),
            Arg.Any<CancellationToken>());
    }

    // ─── Both recipients ─────────────────────────────────────────────────────

    [Fact]
    public async Task Notifies_both_student_and_consultant_with_counterparty_name_and_start_time()
    {
        using var db = CreateDb();
        var startAt = DateTimeOffset.UtcNow.AddHours(12);
        var ids = Seed(db, startAt, studentFirst: "Tasneem", consultantFirst: "Sarah");

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        // Student sees the consultant's name as the counterparty.
        await notifier.Received(1).DispatchAsync(
            ids.StudentId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p =>
                p.CounterpartyName == "Sarah Adel"
                && !string.IsNullOrEmpty(p.StartAtText)),
            $"/student/bookings/{ids.BookingId}",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());

        // Consultant sees the student's name as the counterparty.
        await notifier.Received(1).DispatchAsync(
            ids.ConsultantId, NotificationType.BookingReminder,
            Arg.Is<NotificationParams>(p =>
                p.CounterpartyName == "Tasneem Shaban"
                && !string.IsNullOrEmpty(p.StartAtText)),
            $"/consultant/bookings/{ids.BookingId}",
            Arg.Any<string?>(),
            Arg.Any<CancellationToken>());
    }

    // ─── Non-Confirmed statuses are skipped ──────────────────────────────────

    [Theory]
    [InlineData(BookingStatus.Requested)]
    [InlineData(BookingStatus.Rejected)]
    [InlineData(BookingStatus.Expired)]
    [InlineData(BookingStatus.Cancelled)]
    [InlineData(BookingStatus.Completed)]
    [InlineData(BookingStatus.NoShowStudent)]
    [InlineData(BookingStatus.NoShowConsultant)]
    public async Task Skips_bookings_in_non_confirmed_statuses(BookingStatus status)
    {
        using var db = CreateDb();
        // Within the 24h window but not Confirmed — must be ignored.
        Seed(db, DateTimeOffset.UtcNow.AddHours(12), status: status);

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    // ─── No reminders after the session has started ──────────────────────────

    [Fact]
    public async Task Does_not_send_reminder_after_the_session_start_time()
    {
        using var db = CreateDb();
        // 5 minutes past the scheduled start — even though the booking is still
        // Confirmed (CompletionJob hasn't run yet), reminders must not fire.
        Seed(db, DateTimeOffset.UtcNow.AddMinutes(-5));

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    // ─── Bookings outside the 24h horizon are skipped ────────────────────────

    [Fact]
    public async Task Skips_bookings_more_than_24h_away()
    {
        using var db = CreateDb();
        Seed(db, DateTimeOffset.UtcNow.AddDays(3));

        var notifier = Substitute.For<INotificationDispatcher>();
        await Sut(db, notifier).RunAsync(default);

        await notifier.DidNotReceiveWithAnyArgs().DispatchAsync(
            default, default, default!, default, default, default);
    }

    // ─── Idempotency (real dispatcher, run twice) ────────────────────────────

    [Fact]
    public async Task Does_not_send_duplicate_24h_reminders_across_runs()
    {
        using var db = CreateDb();
        var startAt = DateTimeOffset.UtcNow.AddHours(20);
        var ids = Seed(db, startAt);

        var notifier = new NotificationDispatcher(
            db,
            new NotificationCatalog(),
            Substitute.For<IHubContext<NotificationHub>>(),
            Substitute.For<IEmailService>(),
            NullLogger<NotificationDispatcher>.Instance);

        await Sut(db, notifier).RunAsync(default);
        await Sut(db, notifier).RunAsync(default);

        // First run: 2 recipients × 2 channels (InApp + Email) = 4 rows.
        // Second run is deduped by IdempotencyKey, so the count stays at 4.
        var rows = await db.Notifications
            .Where(n => n.Type == NotificationType.BookingReminder)
            .ToListAsync();
        rows.Should().HaveCount(4);
        rows.Select(r => r.IdempotencyKey).Distinct().Should().HaveCount(2)
            .And.OnlyContain(k => k!.StartsWith($"booking-reminder:{ids.BookingId:N}:24h:"));
    }

    [Fact]
    public async Task Does_not_send_duplicate_1h_reminders_across_runs()
    {
        using var db = CreateDb();
        var startAt = DateTimeOffset.UtcNow.AddMinutes(30);
        var ids = Seed(db, startAt);

        var notifier = new NotificationDispatcher(
            db,
            new NotificationCatalog(),
            Substitute.For<IHubContext<NotificationHub>>(),
            Substitute.For<IEmailService>(),
            NullLogger<NotificationDispatcher>.Instance);

        await Sut(db, notifier).RunAsync(default);
        await Sut(db, notifier).RunAsync(default);

        var rows = await db.Notifications
            .Where(n => n.Type == NotificationType.BookingReminder)
            .ToListAsync();
        rows.Should().HaveCount(4); // 2 recipients × 2 channels, no duplicates
        rows.Select(r => r.IdempotencyKey).Distinct().Should().HaveCount(2)
            .And.OnlyContain(k => k!.StartsWith($"booking-reminder:{ids.BookingId:N}:1h:"));
    }

    // ─── Idempotency keys are distinct per (kind, recipient) ─────────────────

    [Fact]
    public async Task Uses_distinct_idempotency_keys_per_kind_and_recipient()
    {
        using var db = CreateDb();
        var keys = new List<string?>();
        var notifier = Substitute.For<INotificationDispatcher>();
        await notifier.DispatchAsync(
            Arg.Any<Guid>(), Arg.Any<NotificationType>(), Arg.Any<NotificationParams>(),
            Arg.Any<string?>(), Arg.Do<string?>(k => keys.Add(k)), Arg.Any<CancellationToken>());

        // A booking in the 1h window dispatches one pair of (student/consultant)
        // keys — both must be unique.
        var ids = Seed(db, DateTimeOffset.UtcNow.AddMinutes(30));
        await Sut(db, notifier).RunAsync(default);

        keys.Should().HaveCount(2);
        keys.Distinct().Should().HaveCount(2);
        keys.Should().Contain($"booking-reminder:{ids.BookingId:N}:1h:student");
        keys.Should().Contain($"booking-reminder:{ids.BookingId:N}:1h:consultant");
    }
}
