using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Common;

/// <summary>
/// Covers <see cref="ConsultantEligibilityService"/> — the single business rule
/// that decides whether a user may act as a Consultant. Holding the Consultant
/// role is necessary but never sufficient: the account must also be Active and
/// carry an approval signal (verification marker OR approved upgrade request).
/// </summary>
public sealed class ConsultantEligibilityServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public ConsultantEligibilityServiceTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static IUserAdministration Admin(params string[] roles)
    {
        var a = Substitute.For<IUserAdministration>();
        a.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(roles);
        return a;
    }

    private ConsultantEligibilityService Sut(IUserAdministration admin) => new(_db, admin);

    private Guid SeedUser(
        AccountStatus status = AccountStatus.Active,
        DateTimeOffset? consultantVerifiedAt = null)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Test",
            LastName = "User",
            Email = $"{id:N}@test.local",
            UserName = $"{id:N}@test.local",
            AccountStatus = status,
            Profile = new UserProfile
            {
                UserId = id,
                ConsultantVerifiedAt = consultantVerifiedAt,
            },
        });
        _db.SaveChanges();
        return id;
    }

    private void SeedApprovedConsultantUpgrade(Guid userId, bool deleted = false)
    {
        _db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Approved,
            IsDeleted = deleted,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-3),
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Student_only_account_cannot_act_as_consultant()
    {
        var id = SeedUser();

        var result = await Sut(Admin("Student")).CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Consultant_role_without_marker_or_approval_cannot_act()
    {
        // The exact bug: a Consultant role row but no verification and no
        // approved upgrade request.
        var id = SeedUser(consultantVerifiedAt: null);

        var result = await Sut(Admin("Student", "Consultant"))
            .CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Consultant_with_verification_marker_can_act()
    {
        var id = SeedUser(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-10));

        var result = await Sut(Admin("Consultant"))
            .CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Consultant_with_approved_upgrade_but_no_marker_can_act()
    {
        var id = SeedUser(consultantVerifiedAt: null);
        SeedApprovedConsultantUpgrade(id);

        var result = await Sut(Admin("Student", "Consultant"))
            .CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task Soft_deleted_approved_upgrade_does_not_count()
    {
        var id = SeedUser(consultantVerifiedAt: null);
        SeedApprovedConsultantUpgrade(id, deleted: true);

        var result = await Sut(Admin("Consultant"))
            .CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(AccountStatus.Suspended)]
    [InlineData(AccountStatus.Deactivated)]
    [InlineData(AccountStatus.PendingApproval)]
    public async Task Non_active_verified_consultant_cannot_act(AccountStatus status)
    {
        var id = SeedUser(status: status, consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-10));

        var result = await Sut(Admin("Consultant"))
            .CanActAsConsultantAsync(id, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task Overload_with_prefetched_roles_skips_role_lookup()
    {
        var id = SeedUser(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-10));
        var admin = Admin("Consultant");

        var result = await Sut(admin).CanActAsConsultantAsync(
            id, new[] { "Consultant" }, CancellationToken.None);

        result.Should().BeTrue();
        await admin.DidNotReceive().GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    public void Dispose() => _db.Dispose();
}
