using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.StartMeetingRecording;
using ScholarPath.Application.ConsultantBookings.Commands.StoreSessionRecording;
using ScholarPath.Application.ConsultantBookings.Queries.GetBookingRecordings;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// PB-006 — recording a consultant session: starting the recording, storing
/// the finished file (the webhook path), and the access control on viewing.
/// </summary>
public sealed class SessionRecordingTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService User(Guid id, params string[] roles)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(id);
        foreach (var r in roles) u.IsInRole(r).Returns(true);
        return u;
    }

    private static ConsultantBooking Booking(
        Guid studentId, Guid consultantId, BookingStatus status = BookingStatus.Confirmed) => new()
    {
        Id = Guid.NewGuid(),
        StudentId = studentId,
        ConsultantId = consultantId,
        Status = status,
        ScheduledStartAt = DateTimeOffset.UtcNow,
        ScheduledEndAt = DateTimeOffset.UtcNow.AddMinutes(45),
        DurationMinutes = 45,
        PriceUsd = 50m,
    };

    private static IBlobStorageService Storage()
    {
        var s = Substitute.For<IBlobStorageService>();
        s.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("local:session-recordings/rec.mp4");
        return s;
    }

    // ─── Start recording ────────────────────────────────────────────────────

    [Fact]
    public async Task Start_sets_the_recording_fields_then_is_idempotent()
    {
        using var db = CreateDb();
        var booking = Booking(Guid.NewGuid(), Guid.NewGuid());
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new StartMeetingRecordingCommandHandler(
            db, User(booking.ConsultantId), new StubMeetingService());

        await sut.Handle(new StartMeetingRecordingCommand(booking.Id, "server-call-1"), default);
        var afterFirst = await db.Bookings.SingleAsync();
        afterFirst.RecordingStartedAt.Should().NotBeNull();
        afterFirst.RecordingId.Should().NotBeNullOrEmpty();
        var recordingId = afterFirst.RecordingId;

        await sut.Handle(new StartMeetingRecordingCommand(booking.Id, "server-call-1"), default);
        (await db.Bookings.SingleAsync()).RecordingId.Should().Be(recordingId);
    }

    [Fact]
    public async Task Start_is_rejected_for_a_non_participant()
    {
        using var db = CreateDb();
        var booking = Booking(Guid.NewGuid(), Guid.NewGuid());
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new StartMeetingRecordingCommandHandler(
            db, User(Guid.NewGuid()), new StubMeetingService());
        var act = () => sut.Handle(new StartMeetingRecordingCommand(booking.Id, "x"), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ─── Store recording (recording-ready webhook path) ─────────────────────

    [Fact]
    public async Task Store_links_a_recording_to_its_booking_and_is_idempotent()
    {
        using var db = CreateDb();
        var booking = Booking(Guid.NewGuid(), Guid.NewGuid());
        booking.RecordingId = "acs-rec-1";
        booking.RecordingStartedAt = DateTimeOffset.UtcNow;
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new StoreSessionRecordingCommandHandler(
            db, new StubMeetingService(), Storage(),
            NullLogger<StoreSessionRecordingCommandHandler>.Instance);

        // A redelivered Event Grid event must not store the recording twice.
        await sut.Handle(new StoreSessionRecordingCommand("acs-rec-1", "https://acs/content"), default);
        await sut.Handle(new StoreSessionRecordingCommand("acs-rec-1", "https://acs/content"), default);

        var recordings = await db.SessionRecordings.ToListAsync();
        recordings.Should().ContainSingle()
            .Which.BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public async Task Store_skips_a_recording_with_no_matching_booking()
    {
        using var db = CreateDb();
        var sut = new StoreSessionRecordingCommandHandler(
            db, new StubMeetingService(), Storage(),
            NullLogger<StoreSessionRecordingCommandHandler>.Instance);

        await sut.Handle(new StoreSessionRecordingCommand("unknown-rec", "https://acs/content"), default);

        (await db.SessionRecordings.AnyAsync()).Should().BeFalse();
    }

    // ─── Viewing access control ─────────────────────────────────────────────

    [Fact]
    public async Task GetRecordings_allows_a_participant_and_blocks_an_outsider()
    {
        using var db = CreateDb();
        var booking = Booking(Guid.NewGuid(), Guid.NewGuid());
        db.Bookings.Add(booking);
        db.SessionRecordings.Add(new SessionRecording
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            RecordingId = "r1",
            StoragePath = "local:session-recordings/r1.mp4",
            ContentType = "video/mp4",
            SizeBytes = 1024,
            RecordedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();

        var asStudent = new GetBookingRecordingsQueryHandler(db, User(booking.StudentId));
        (await asStudent.Handle(new GetBookingRecordingsQuery(booking.Id), default))
            .Should().ContainSingle();

        var asOutsider = new GetBookingRecordingsQueryHandler(db, User(Guid.NewGuid()));
        var act = () => asOutsider.Handle(new GetBookingRecordingsQuery(booking.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetRecordings_allows_an_admin()
    {
        using var db = CreateDb();
        var booking = Booking(Guid.NewGuid(), Guid.NewGuid());
        db.Bookings.Add(booking);
        await db.SaveChangesAsync();

        var sut = new GetBookingRecordingsQueryHandler(db, User(Guid.NewGuid(), "Admin"));
        var result = await sut.Handle(new GetBookingRecordingsQuery(booking.Id), default);

        result.Should().BeEmpty();
    }
}
