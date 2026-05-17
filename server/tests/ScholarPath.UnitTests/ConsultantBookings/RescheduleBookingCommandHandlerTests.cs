using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;
using FluentAssertions;

namespace ScholarPath.UnitTests.ConsultantBookings;

public sealed class RescheduleBookingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly RescheduleBookingCommandHandler _handler;

    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public RescheduleBookingCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);
        _handler = new RescheduleBookingCommandHandler(_db, _currentUser, _publisher);
    }

    private async Task<ConsultantBooking> SeedBookingAsync(
        BookingStatus status,
        DateTimeOffset start,
        int durationMinutes = 60)
    {
        var booking = new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = _studentId,
            ConsultantId = _consultantId,
            ScheduledStartAt = start,
            ScheduledEndAt = start.AddMinutes(durationMinutes),
            DurationMinutes = durationMinutes,
            PriceUsd = 50m,
            Status = status,
            StripePaymentIntentId = "pi_test",
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking;
    }

    [Fact]
    public async Task Handle_ConfirmedBooking_MovesTimesAndPublishesEvent()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedBookingAsync(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var command = new RescheduleBookingCommand(
            booking.Id, null, newStart, newStart.AddMinutes(60));

        await _handler.Handle(command, CancellationToken.None);

        var updated = await _db.Bookings.FirstAsync(b => b.Id == booking.Id);
        updated.ScheduledStartAt.Should().Be(newStart.ToUniversalTime());
        updated.ScheduledEndAt.Should().Be(newStart.AddMinutes(60).ToUniversalTime());
        updated.Status.Should().Be(BookingStatus.Confirmed);

        await _publisher.Received(1).Publish(
            Arg.Any<BookingRescheduledEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TerminalBooking_ThrowsBookingDomainException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedBookingAsync(BookingStatus.Completed, DateTimeOffset.UtcNow.AddDays(2));

        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var command = new RescheduleBookingCommand(
            booking.Id, null, newStart, newStart.AddMinutes(60));

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BookingDomainException>();
    }

    [Fact]
    public async Task Handle_DurationChanged_ThrowsBookingDomainException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedBookingAsync(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        // 90 minutes — does not match the 60-minute original.
        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var command = new RescheduleBookingCommand(
            booking.Id, null, newStart, newStart.AddMinutes(90));

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BookingDomainException>();
    }

    [Fact]
    public async Task Handle_TimeInThePast_ThrowsBookingDomainException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedBookingAsync(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        var pastStart = DateTimeOffset.UtcNow.AddDays(-1);
        var command = new RescheduleBookingCommand(
            booking.Id, null, pastStart, pastStart.AddMinutes(60));

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BookingDomainException>();
    }

    [Fact]
    public async Task Handle_ConsultantHasOverlappingBooking_ThrowsBookingDomainException()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(_studentId);
        var booking = await SeedBookingAsync(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        // A second confirmed booking for the same consultant at the target time.
        var clashStart = DateTimeOffset.UtcNow.AddDays(5);
        _db.Bookings.Add(new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = Guid.NewGuid(),
            ConsultantId = _consultantId,
            ScheduledStartAt = clashStart,
            ScheduledEndAt = clashStart.AddMinutes(60),
            DurationMinutes = 60,
            PriceUsd = 50m,
            Status = BookingStatus.Confirmed,
            StripePaymentIntentId = "pi_other",
        });
        await _db.SaveChangesAsync();

        var command = new RescheduleBookingCommand(
            booking.Id, null, clashStart, clashStart.AddMinutes(60));

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BookingDomainException>();
    }

    [Fact]
    public async Task Handle_NotAParticipant_ThrowsUnauthorized()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.UserId.Returns(Guid.NewGuid()); // some other user
        var booking = await SeedBookingAsync(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        var newStart = DateTimeOffset.UtcNow.AddDays(5);
        var command = new RescheduleBookingCommand(
            booking.Id, null, newStart, newStart.AddMinutes(60));

        await _handler.Awaiting(h => h.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _db.Dispose();
}
