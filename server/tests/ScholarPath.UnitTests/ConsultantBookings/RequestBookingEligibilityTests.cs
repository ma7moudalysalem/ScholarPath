using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Services;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// A student may only book a consultant who is truly eligible (in-role, Active,
/// verified/approved) — this guards the direct-API booking path against a
/// stale/unapproved Consultant-role account that the marketplace already hides.
/// </summary>
public sealed class RequestBookingEligibilityTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser = Substitute.For<ICurrentUserService>();
    private readonly IStripeService _stripe = Substitute.For<IStripeService>();
    private readonly IPublisher _publisher = Substitute.For<IPublisher>();
    private readonly Guid _studentId = Guid.NewGuid();
    private readonly Guid _consultantId = Guid.NewGuid();

    public RequestBookingEligibilityTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        _currentUser.IsAuthenticated.Returns(true);
        _currentUser.IsInRole("Student").Returns(true);
        _currentUser.UserId.Returns(_studentId);
    }

    public void Dispose() => _db.Dispose();

    private static IUserAdministration Admin(params string[] roles)
    {
        var a = Substitute.For<IUserAdministration>();
        a.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(roles);
        return a;
    }

    private void SeedConsultant(DateTimeOffset? consultantVerifiedAt)
    {
        _db.Users.Add(new ApplicationUser
        {
            Id = _consultantId,
            FirstName = "Sarah",
            LastName = "Adel",
            Email = "c@test.local",
            UserName = "c@test.local",
            AccountStatus = AccountStatus.Active,
            Profile = new UserProfile
            {
                UserId = _consultantId,
                SessionFeeUsd = 0m,            // free path → no Stripe dependency
                SessionDurationMinutes = null,
                ConsultantVerifiedAt = consultantVerifiedAt,
            },
        });
        _db.SaveChanges();
    }

    private RequestBookingCommand FutureBooking()
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        return new RequestBookingCommand(_consultantId, null, start, start.AddMinutes(45), "UTC", null);
    }

    private RequestBookingCommandHandler Sut(IUserAdministration admin) =>
        new(_db, _currentUser, _stripe, _publisher, new ConsultantEligibilityService(_db, admin));

    [Fact]
    public async Task Booking_an_unverified_consultant_is_rejected()
    {
        // In the Consultant role but no verification marker / approved upgrade.
        SeedConsultant(consultantVerifiedAt: null);

        var act = () => Sut(Admin("Consultant")).Handle(FutureBooking(), CancellationToken.None);

        await act.Should().ThrowAsync<BookingDomainException>().WithMessage("*not available*");
        _db.Bookings.Should().BeEmpty();
    }

    [Fact]
    public async Task Booking_a_consultant_without_the_role_is_rejected()
    {
        // Even with a verification marker, a user not actually in the Consultant
        // role is not bookable.
        SeedConsultant(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-10));

        var act = () => Sut(Admin("Student")).Handle(FutureBooking(), CancellationToken.None);

        await act.Should().ThrowAsync<BookingDomainException>().WithMessage("*not available*");
        _db.Bookings.Should().BeEmpty();
    }

    [Fact]
    public async Task Booking_a_verified_consultant_succeeds()
    {
        SeedConsultant(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-30));

        var result = await Sut(Admin("Consultant")).Handle(FutureBooking(), CancellationToken.None);

        result.BookingId.Should().NotBeEmpty();
        _db.Bookings.Should().ContainSingle();
    }
}
