using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.RecordMeetingJoin;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// FR-217 — automated no-show detection: a participant's join timestamp is the
/// attendance signal, and <see cref="MeetingNoShowSweepJob"/> attributes a
/// no-show to whichever party never joined.
/// </summary>
public sealed class MeetingNoShowTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static INotificationDispatcher Notifications() =>
        Substitute.For<INotificationDispatcher>();

    private static ConsultantBooking Booking(
        DateTimeOffset endAt,
        DateTimeOffset? studentJoined,
        DateTimeOffset? consultantJoined,
        BookingStatus status = BookingStatus.Confirmed)
    {
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 10000,
            Currency = "USD",
            IdempotencyKey = "ik-" + Guid.NewGuid().ToString("N"),
            PayerUserId = Guid.NewGuid(),
            PayeeAmountCents = 9000,
            ProfitShareAmountCents = 1000,
        };
        return new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ConsultantId = Guid.NewGuid(),
            Status = status,
            ScheduledStartAt = endAt.AddMinutes(-45),
            ScheduledEndAt = endAt,
            DurationMinutes = 45,
            PriceUsd = 100m,
            StripePaymentIntentId = "pi-" + Guid.NewGuid().ToString("N"),
            StudentJoinedAt = studentJoined,
            ConsultantJoinedAt = consultantJoined,
            Payment = payment,
        };
    }

    // ─── No-show sweep job ──────────────────────────────────────────────────

    [Fact]
    public async Task Sweep_files_a_consultant_no_show_report_when_only_the_student_joined()
    {
        using var db = CreateDb();
        var ended = DateTimeOffset.UtcNow.AddMinutes(-30);
        var booking = Booking(ended, studentJoined: ended.AddMinutes(-40), consultantJoined: null);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new MeetingNoShowSweepJob(db, Notifications(), NullLogger<MeetingNoShowSweepJob>.Instance);
        await sut.RunAsync(default);

        // PB-006R: the sweep freezes the booking pending admin validation — NO
        // penalty or refund is applied here.
        var saved = await db.Bookings.Include(b => b.Payment).SingleAsync();
        saved.Status.Should().Be(BookingStatus.NoShowReported);
        saved.NoShowMarkedAt.Should().NotBeNull();
        saved.IsNoShowConsultant.Should().BeFalse();
        saved.Payment!.Status.Should().Be(PaymentStatus.Captured);

        var report = await db.NoShowReports.SingleAsync();
        report.ReporterUserId.Should().Be(booking.StudentId);
        report.AccusedUserId.Should().Be(booking.ConsultantId);
        report.AccusedRole.Should().Be(NoShowAccusedRole.Consultant);
        report.Status.Should().Be(NoShowReportStatus.PendingReview);
    }

    [Fact]
    public async Task Sweep_files_a_student_no_show_report_when_only_the_consultant_joined()
    {
        using var db = CreateDb();
        var ended = DateTimeOffset.UtcNow.AddMinutes(-30);
        var booking = Booking(ended, studentJoined: null, consultantJoined: ended.AddMinutes(-40));
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new MeetingNoShowSweepJob(db, Notifications(), NullLogger<MeetingNoShowSweepJob>.Instance);
        await sut.RunAsync(default);

        var saved = await db.Bookings.SingleAsync();
        saved.Status.Should().Be(BookingStatus.NoShowReported);
        saved.IsNoShowStudent.Should().BeFalse();

        var report = await db.NoShowReports.SingleAsync();
        report.ReporterUserId.Should().Be(booking.ConsultantId);
        report.AccusedUserId.Should().Be(booking.StudentId);
        report.AccusedRole.Should().Be(NoShowAccusedRole.Student);
        report.Status.Should().Be(NoShowReportStatus.PendingReview);
    }

    [Fact]
    public async Task Sweep_leaves_a_booking_both_parties_joined_untouched()
    {
        using var db = CreateDb();
        var ended = DateTimeOffset.UtcNow.AddMinutes(-30);
        db.Bookings.Add(Booking(ended, studentJoined: ended, consultantJoined: ended));
        await db.SaveChangesAsync();

        var sut = new MeetingNoShowSweepJob(db, Notifications(), NullLogger<MeetingNoShowSweepJob>.Instance);
        await sut.RunAsync(default);

        (await db.Bookings.SingleAsync()).Status.Should().Be(BookingStatus.Confirmed);
        (await db.NoShowReports.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Sweep_leaves_a_booking_nobody_joined_untouched_so_a_missing_signal_is_never_a_no_show()
    {
        using var db = CreateDb();
        var ended = DateTimeOffset.UtcNow.AddMinutes(-30);
        db.Bookings.Add(Booking(ended, studentJoined: null, consultantJoined: null));
        await db.SaveChangesAsync();

        var sut = new MeetingNoShowSweepJob(db, Notifications(), NullLogger<MeetingNoShowSweepJob>.Instance);
        await sut.RunAsync(default);

        var saved = await db.Bookings.SingleAsync();
        saved.Status.Should().Be(BookingStatus.Confirmed);
        (await db.NoShowReports.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Sweep_leaves_a_booking_still_inside_the_post_session_grace_untouched()
    {
        using var db = CreateDb();
        // Ended only 5 minutes ago — inside the 20-minute grace, a late join
        // may still be recorded, so the sweep must not judge it yet.
        var ended = DateTimeOffset.UtcNow.AddMinutes(-5);
        db.Bookings.Add(Booking(ended, studentJoined: ended.AddMinutes(-40), consultantJoined: null));
        await db.SaveChangesAsync();

        var sut = new MeetingNoShowSweepJob(db, Notifications(), NullLogger<MeetingNoShowSweepJob>.Instance);
        await sut.RunAsync(default);

        (await db.Bookings.SingleAsync()).Status.Should().Be(BookingStatus.Confirmed);
    }

    // ─── Record-meeting-join command ────────────────────────────────────────

    private static ICurrentUserService User(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(id);
        return u;
    }

    [Fact]
    public async Task Join_records_the_timestamp_for_a_participant_inside_the_window()
    {
        using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var booking = Booking(now.AddMinutes(35), studentJoined: null, consultantJoined: null);
        booking.ScheduledStartAt = now.AddMinutes(-10); // session in progress
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new RecordMeetingJoinCommandHandler(db, User(booking.StudentId), new StubMeetingService());
        var result = await sut.Handle(new RecordMeetingJoinCommand(booking.Id), default);

        result.BookingId.Should().Be(booking.Id);
        (await db.Bookings.SingleAsync()).StudentJoinedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Join_is_rejected_for_a_non_participant()
    {
        using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var booking = Booking(now.AddMinutes(35), studentJoined: null, consultantJoined: null);
        booking.ScheduledStartAt = now.AddMinutes(-10);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new RecordMeetingJoinCommandHandler(db, User(Guid.NewGuid()), new StubMeetingService());
        var act = () => sut.Handle(new RecordMeetingJoinCommand(booking.Id), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Join_keeps_the_first_timestamp_on_a_re_join()
    {
        using var db = CreateDb();
        var now = DateTimeOffset.UtcNow;
        var firstJoin = now.AddMinutes(-8);
        var booking = Booking(now.AddMinutes(35), studentJoined: firstJoin, consultantJoined: null);
        booking.ScheduledStartAt = now.AddMinutes(-10);
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new RecordMeetingJoinCommandHandler(db, User(booking.StudentId), new StubMeetingService());
        await sut.Handle(new RecordMeetingJoinCommand(booking.Id), default);

        (await db.Bookings.SingleAsync()).StudentJoinedAt.Should().Be(firstJoin);
    }
}
