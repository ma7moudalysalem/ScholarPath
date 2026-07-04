using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// PB-006R (FR-CBR-15..20): a &lt;24h cancellation of a confirmed booking penalises
/// the cancelling party — 3-day student block / 20% consultant rating deduction.
/// Exercised via the free-booking path (no Stripe / financial config needed).
/// </summary>
public sealed class CancelBookingPenaltyTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public CancelBookingPenaltyTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _currentUser.IsAuthenticated.Returns(true);
    }

    private CancelBookingCommandHandler Sut()
    {
        var rating = new ConsultantRatingService(
            _db, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConsultantRatingService>.Instance);
        return new CancelBookingCommandHandler(
            _db, _currentUser, Substitute.For<IStripeService>(), new RefundCalculatorService(),
            rating, Options.Create(new BookingOptions()), Substitute.For<IPublisher>());
    }

    private async Task<Guid> SeedConfirmedFreeBookingAsync(int startInHours)
    {
        _db.UserProfiles.Add(new UserProfile { UserId = _studentId });
        _db.UserProfiles.Add(new UserProfile { UserId = _consultantId });
        var booking = new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = _studentId,
            ConsultantId = _consultantId,
            Status = BookingStatus.Confirmed,
            ScheduledStartAt = DateTimeOffset.UtcNow.AddHours(startInHours),
            ScheduledEndAt = DateTimeOffset.UtcNow.AddHours(startInHours + 1),
            DurationMinutes = 45,
            PriceUsd = 0m,
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();
        return booking.Id;
    }

    private async Task<UserProfile> ProfileAsync(Guid userId) =>
        await _db.UserProfiles.AsNoTracking().FirstAsync(p => p.UserId == userId);

    [Fact]
    public async Task Student_cancel_within_24h_blocks_the_student_for_3_days()
    {
        var bookingId = await SeedConfirmedFreeBookingAsync(startInHours: 12);
        _currentUser.UserId.Returns(_studentId);

        await Sut().Handle(new CancelBookingCommand(bookingId), default);

        var student = await ProfileAsync(_studentId);
        student.BookingAccessStatus.Should().Be(BookingAccessStatus.BookingBlocked);
        student.BookingBlockReason.Should().Be(BookingBlockReason.CancelledLessThan24Hours);
        student.BookingBlockUntil.Should().BeCloseTo(DateTimeOffset.UtcNow.AddDays(3), TimeSpan.FromMinutes(2));
    }

    [Fact]
    public async Task Student_cancel_more_than_24h_does_not_block()
    {
        var bookingId = await SeedConfirmedFreeBookingAsync(startInHours: 48);
        _currentUser.UserId.Returns(_studentId);

        await Sut().Handle(new CancelBookingCommand(bookingId), default);

        (await ProfileAsync(_studentId)).BookingAccessStatus.Should().Be(BookingAccessStatus.Active);
    }

    [Fact]
    public async Task Consultant_cancel_within_24h_deducts_20_percent_from_rating()
    {
        var bookingId = await SeedConfirmedFreeBookingAsync(startInHours: 12);
        _currentUser.UserId.Returns(_consultantId);

        await Sut().Handle(new CancelBookingCommand(bookingId), default);

        (await ProfileAsync(_consultantId)).ConsultantRatingPenaltyFactor.Should().Be(0.80m);
    }

    [Fact]
    public async Task Consultant_cancel_more_than_24h_does_not_deduct()
    {
        var bookingId = await SeedConfirmedFreeBookingAsync(startInHours: 48);
        _currentUser.UserId.Returns(_consultantId);

        await Sut().Handle(new CancelBookingCommand(bookingId), default);

        (await ProfileAsync(_consultantId)).ConsultantRatingPenaltyFactor.Should().Be(1.0m);
    }

    public void Dispose() => _db.Dispose();
}
