using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Audit H8: CompletionJob must NOT auto-complete a booking where exactly one party
/// joined — that's a no-show and belongs to MeetingNoShowSweepJob. Completing it here
/// would silently erase the present party's no-show remedy.
/// </summary>
public sealed class CompletionJobTests : IDisposable
{
    private readonly ApplicationDbContext _db = new(
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private CompletionJob Sut() =>
        new(_db, NullLogger<CompletionJob>.Instance, Substitute.For<IPublisher>());

    private ConsultantBooking Booking(DateTimeOffset? studentJoined, DateTimeOffset? consultantJoined)
    {
        var ended = DateTimeOffset.UtcNow.AddHours(-7); // past the 6h completion threshold
        return new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ConsultantId = Guid.NewGuid(),
            Status = BookingStatus.Confirmed,
            ScheduledStartAt = ended.AddMinutes(-45),
            ScheduledEndAt = ended,
            DurationMinutes = 45,
            StudentJoinedAt = studentJoined,
            ConsultantJoinedAt = consultantJoined,
        };
    }

    [Fact]
    public async Task Does_not_complete_a_one_party_joined_booking()
    {
        var now = DateTimeOffset.UtcNow;
        var b = Booking(studentJoined: now.AddHours(-8), consultantJoined: null);
        _db.Bookings.Add(b);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Bookings.AsNoTracking().FirstAsync()).Status
            .Should().Be(BookingStatus.Confirmed); // left for the no-show sweep
    }

    [Fact]
    public async Task Completes_a_both_parties_joined_booking()
    {
        var now = DateTimeOffset.UtcNow;
        var b = Booking(studentJoined: now.AddHours(-8), consultantJoined: now.AddHours(-8));
        _db.Bookings.Add(b);
        await _db.SaveChangesAsync();

        await Sut().RunAsync(default);

        (await _db.Bookings.AsNoTracking().FirstAsync()).Status
            .Should().Be(BookingStatus.Completed);
    }

    public void Dispose() => _db.Dispose();
}
