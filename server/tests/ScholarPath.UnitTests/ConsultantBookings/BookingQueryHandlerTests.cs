using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetBookingById;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantBookings;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyBookings;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Covers the booking + availability read queries that run directly against
/// <see cref="IApplicationDbContext"/> (no Identity role join needed):
/// <see cref="GetMyBookingsQuery"/>, <see cref="GetConsultantBookingsQuery"/>,
/// <see cref="GetBookingByIdQuery"/> and <see cref="GetMyAvailabilityQuery"/>.
/// Uses the EF Core in-memory provider — same approach as
/// <see cref="RescheduleBookingCommandHandlerTests"/>.
/// </summary>
public sealed class BookingQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();

    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public BookingQueryHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        _db.Users.Add(NewUser(_studentId, "Tasneem", "Shaban", "tasneem@test.com"));
        _db.Users.Add(NewUser(_consultantId, "Sarah", "Adel", "sarah@test.com"));
        _db.SaveChanges();
    }

    private static ApplicationUser NewUser(Guid id, string first, string last, string email) => new()
    {
        Id = id,
        FirstName = first,
        LastName = last,
        Email = email,
        UserName = email,
        ProfileImageUrl = $"https://img/{first}.png",
        AccountStatus = AccountStatus.Active,
    };

    private ConsultantBooking SeedBooking(
        BookingStatus status,
        DateTimeOffset start,
        Guid? studentId = null,
        Guid? consultantId = null,
        int durationMinutes = 45)
    {
        var booking = new ConsultantBooking
        {
            Id = Guid.NewGuid(),
            StudentId = studentId ?? _studentId,
            ConsultantId = consultantId ?? _consultantId,
            ScheduledStartAt = start,
            ScheduledEndAt = start.AddMinutes(durationMinutes),
            DurationMinutes = durationMinutes,
            PriceUsd = 35m,
            Status = status,
            RequestedAt = start.AddDays(-3),
            StripePaymentIntentId = "pi_test",
        };
        _db.Bookings.Add(booking);
        _db.SaveChanges();
        return booking;
    }

    // ── GetMyBookingsQuery ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetMyBookings_ReturnsOnlyTheCallersBookings_NewestFirst()
    {
        var other = Guid.NewGuid();
        _db.Users.Add(NewUser(other, "Other", "Student", "other@test.com"));
        _db.SaveChanges();

        SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));
        SeedBooking(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(8));
        SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(5), studentId: other);

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetMyBookingsQueryHandler(_db, _currentUser);

        var result = await handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        result.Should().HaveCount(2);
        result[0].ScheduledStartAt.Should().BeAfter(result[1].ScheduledStartAt);
        result.Should().OnlyContain(b => b.StudentId == _studentId);
        result[0].ConsultantName.Should().Be("Sarah Adel");
        result[0].StudentName.Should().Be("Tasneem Shaban");
    }

    [Fact]
    public async Task GetMyBookings_ExcludesSoftDeletedBookings()
    {
        var deleted = SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));
        deleted.IsDeleted = true;
        await _db.SaveChangesAsync();

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetMyBookingsQueryHandler(_db, _currentUser);

        var result = await handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyBookings_ThrowsForbidden_WhenNotAuthenticated()
    {
        _currentUser.UserId.Returns((Guid?)null);
        var handler = new GetMyBookingsQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(new GetMyBookingsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── GetConsultantBookingsQuery ──────────────────────────────────────────────

    [Fact]
    public async Task GetConsultantBookings_ReturnsOnlyTheConsultantsIncomingBookings()
    {
        var otherConsultant = Guid.NewGuid();
        _db.Users.Add(NewUser(otherConsultant, "Ahmed", "Mostafa", "ahmed@test.com"));
        _db.SaveChanges();

        SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));
        SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(4),
            consultantId: otherConsultant);

        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);
        var handler = new GetConsultantBookingsQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetConsultantBookingsQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].ConsultantId.Should().Be(_consultantId);
    }

    [Fact]
    public async Task GetConsultantBookings_ThrowsForbidden_WhenCallerIsNotAConsultant()
    {
        _currentUser.IsInRole("Consultant").Returns(false);
        _currentUser.UserId.Returns(_studentId);
        var handler = new GetConsultantBookingsQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(new GetConsultantBookingsQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // ── GetBookingByIdQuery ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetBookingById_ReturnsFullDetail_WhenCallerIsTheStudent()
    {
        var booking = SeedBooking(BookingStatus.Confirmed, DateTimeOffset.UtcNow.AddDays(2));

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        result.Id.Should().Be(booking.Id);
        result.Status.Should().Be(BookingStatus.Confirmed);
        result.ConsultantEmail.Should().Be("sarah@test.com");
        result.HasStudentReview.Should().BeFalse();
    }

    [Fact]
    public async Task GetBookingById_HasStudentReview_IsTrue_WhenAVisibleReviewExists()
    {
        var booking = SeedBooking(BookingStatus.Completed, DateTimeOffset.UtcNow.AddDays(-1));
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            StudentId = _studentId,
            ConsultantId = _consultantId,
            Rating = 5,
            Comment = "Great session",
        });
        await _db.SaveChangesAsync();

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        result.HasStudentReview.Should().BeTrue();
    }

    [Fact]
    public async Task GetBookingById_HasStudentReview_IsFalse_WhenTheOnlyReviewIsSoftDeleted()
    {
        var booking = SeedBooking(BookingStatus.Completed, DateTimeOffset.UtcNow.AddDays(-1));
        _db.ConsultantReviews.Add(new ConsultantReview
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            StudentId = _studentId,
            ConsultantId = _consultantId,
            Rating = 4,
            IsDeleted = true,
            DeletedAt = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync();

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        result.HasStudentReview.Should().BeFalse();
    }

    [Fact]
    public async Task GetBookingById_ReturnsDetail_WhenCallerIsTheConsultant()
    {
        var booking = SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));

        _currentUser.UserId.Returns(_consultantId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBookingById_ReturnsDetail_WhenCallerIsAdmin()
    {
        var booking = SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));

        _currentUser.UserId.Returns(Guid.NewGuid()); // neither participant
        _currentUser.IsInRole("Admin").Returns(true);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var result = await handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        result.Id.Should().Be(booking.Id);
    }

    [Fact]
    public async Task GetBookingById_ThrowsForbidden_WhenCallerIsAnUnrelatedUser()
    {
        var booking = SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));

        _currentUser.UserId.Returns(Guid.NewGuid());
        _currentUser.IsInRole(Arg.Any<string>()).Returns(false);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetBookingById_ThrowsNotFound_WhenBookingDoesNotExist()
    {
        _currentUser.UserId.Returns(_studentId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(
            new GetBookingByIdQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetBookingById_ThrowsNotFound_WhenBookingIsSoftDeleted()
    {
        var booking = SeedBooking(BookingStatus.Requested, DateTimeOffset.UtcNow.AddDays(2));
        booking.IsDeleted = true;
        await _db.SaveChangesAsync();

        _currentUser.UserId.Returns(_studentId);
        var handler = new GetBookingByIdQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(
            new GetBookingByIdQuery(booking.Id), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── GetMyAvailabilityQuery ──────────────────────────────────────────────────

    [Fact]
    public async Task GetMyAvailability_ReturnsOnlyTheConsultantsActiveRules()
    {
        _db.Availabilities.Add(new ConsultantAvailability
        {
            Id = Guid.NewGuid(), ConsultantId = _consultantId,
            IsRecurring = true, DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(20, 0),
            Timezone = "UTC", IsActive = true,
        });
        _db.Availabilities.Add(new ConsultantAvailability
        {
            Id = Guid.NewGuid(), ConsultantId = _consultantId,
            IsRecurring = true, DayOfWeek = DayOfWeek.Tuesday,
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(12, 0),
            Timezone = "UTC", IsActive = false, // inactive — excluded
        });
        _db.Availabilities.Add(new ConsultantAvailability
        {
            Id = Guid.NewGuid(), ConsultantId = Guid.NewGuid(), // other consultant
            IsRecurring = true, DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(9, 0), EndTime = new TimeOnly(11, 0),
            Timezone = "UTC", IsActive = true,
        });
        await _db.SaveChangesAsync();

        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);
        var handler = new GetMyAvailabilityQueryHandler(_db, _currentUser);

        var result = await handler.Handle(new GetMyAvailabilityQuery(), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].DayOfWeek.Should().Be(DayOfWeek.Monday);
        result[0].ConsultantId.Should().Be(_consultantId);
    }

    [Fact]
    public async Task GetMyAvailability_ThrowsForbidden_WhenCallerIsNotAConsultant()
    {
        _currentUser.IsInRole("Consultant").Returns(false);
        _currentUser.UserId.Returns(_studentId);
        var handler = new GetMyAvailabilityQueryHandler(_db, _currentUser);

        var act = () => handler.Handle(new GetMyAvailabilityQuery(), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    // A weekday may now carry more than one recurring window. Saving several
    // slots for the same day must persist each as its own rule and read back
    // intact — this guards the multi-slot-per-day editor end to end.
    [Fact]
    public async Task UpdateThenGetMyAvailability_RoundTripsMultipleRecurringSlotsOnOneDay()
    {
        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Consultant").Returns(true);
        _currentUser.UserId.Returns(_consultantId);

        // Eligibility is covered by UpdateAvailabilityCommandHandlerTests; this
        // test only exercises the multi-slot round-trip, so treat the consultant
        // as eligible.
        var eligibility = Substitute.For<IConsultantEligibilityService>();
        eligibility.CanActAsConsultantAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var update = new UpdateAvailabilityCommandHandler(_db, _currentUser, eligibility);
        await update.Handle(
            new UpdateAvailabilityCommand(
                ReplaceExisting: true,
                Slots:
                [
                    Recurring(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(11, 0)),
                    Recurring(DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(16, 0)),
                    Recurring(DayOfWeek.Wednesday, new TimeOnly(18, 0), new TimeOnly(20, 0)),
                ]),
            CancellationToken.None);

        var query = new GetMyAvailabilityQueryHandler(_db, _currentUser);
        var result = await query.Handle(new GetMyAvailabilityQuery(), CancellationToken.None);

        result.Should().HaveCount(3);
        result.Should().OnlyContain(r => r.ConsultantId == _consultantId && r.IsActive && r.IsRecurring);

        var monday = result.Where(r => r.DayOfWeek == DayOfWeek.Monday).ToList();
        monday.Should().HaveCount(2);
        monday.Select(r => r.StartTime).Should().BeEquivalentTo(
            new TimeOnly?[] { new TimeOnly(9, 0), new TimeOnly(14, 0) });
        monday.Select(r => r.EndTime).Should().BeEquivalentTo(
            new TimeOnly?[] { new TimeOnly(11, 0), new TimeOnly(16, 0) });
    }

    private static AvailabilityInputModel Recurring(DayOfWeek day, TimeOnly start, TimeOnly end) =>
        new(
            IsRecurring: true,
            DayOfWeek: day,
            StartTime: start,
            EndTime: end,
            SpecificStartAt: null,
            SpecificEndAt: null,
            Timezone: "Africa/Cairo",
            IsActive: true);

    public void Dispose() => _db.Dispose();
}
