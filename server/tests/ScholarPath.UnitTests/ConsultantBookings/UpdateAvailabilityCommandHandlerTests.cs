using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Services;
using ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.ConsultantBookings;

/// <summary>
/// Covers the consultant-eligibility gate on <see cref="UpdateAvailabilityCommandHandler"/>:
/// a Consultant role row is not enough — the account must be a verified/approved
/// consultant to publish or edit availability.
/// </summary>
public sealed class UpdateAvailabilityCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public UpdateAvailabilityCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static ICurrentUserService CurrentUser(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.IsAuthenticated.Returns(true);
        u.UserId.Returns(id);
        u.IsInRole("Consultant").Returns(true);
        return u;
    }

    private static IUserAdministration Admin(params string[] roles)
    {
        var a = Substitute.For<IUserAdministration>();
        a.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(roles);
        return a;
    }

    private UpdateAvailabilityCommandHandler Sut(Guid userId, IUserAdministration admin) =>
        new(_db, CurrentUser(userId), new ConsultantEligibilityService(_db, admin));

    private Guid SeedConsultant(DateTimeOffset? consultantVerifiedAt)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Hana",
            LastName = "Farouk",
            Email = $"{id:N}@test.local",
            UserName = $"{id:N}@test.local",
            AccountStatus = AccountStatus.Active,
            Profile = new UserProfile { UserId = id, ConsultantVerifiedAt = consultantVerifiedAt },
        });
        _db.SaveChanges();
        return id;
    }

    private static UpdateAvailabilityCommand OneRecurringSlot() => new(
        ReplaceExisting: true,
        Slots: new List<AvailabilityInputModel>
        {
            new(
                IsRecurring: true,
                DayOfWeek: DayOfWeek.Tuesday,
                StartTime: new TimeOnly(16, 0),
                EndTime: new TimeOnly(18, 0),
                SpecificStartAt: null,
                SpecificEndAt: null,
                Timezone: "Africa/Cairo",
                IsActive: true),
        });

    [Fact]
    public async Task Unverified_consultant_role_cannot_update_availability()
    {
        var id = SeedConsultant(consultantVerifiedAt: null);

        var act = () => Sut(id, Admin("Student", "Consultant"))
            .Handle(OneRecurringSlot(), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*verified consultants*");

        (await _db.Availabilities.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Verified_consultant_can_update_availability()
    {
        var id = SeedConsultant(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-20));

        var result = await Sut(id, Admin("Consultant")).Handle(OneRecurringSlot(), default);

        result.Should().Be(Unit.Value);
        var saved = await _db.Availabilities.SingleAsync();
        saved.ConsultantId.Should().Be(id);
        saved.IsRecurring.Should().BeTrue();
        saved.DayOfWeek.Should().Be(DayOfWeek.Tuesday);
    }

    [Fact]
    public async Task Unauthenticated_user_is_rejected()
    {
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.IsAuthenticated.Returns(false);
        var handler = new UpdateAvailabilityCommandHandler(
            _db, currentUser, new ConsultantEligibilityService(_db, Admin()));

        var act = () => handler.Handle(OneRecurringSlot(), default);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    public void Dispose() => _db.Dispose();
}
