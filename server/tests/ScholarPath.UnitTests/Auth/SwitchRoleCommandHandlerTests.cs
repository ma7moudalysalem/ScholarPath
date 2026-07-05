using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.SwitchRole;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Services;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

/// <summary>
/// Covers <see cref="SwitchRoleCommandHandler"/> — in particular the consultant
/// eligibility gate: switching to the Consultant role requires verified/approved
/// consultant status, not merely a Consultant role row.
/// </summary>
public sealed class SwitchRoleCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public SwitchRoleCommandHandlerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private static ICurrentUserService CurrentUser(Guid? id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static IUserAdministration Admin(params string[] roles)
    {
        var a = Substitute.For<IUserAdministration>();
        a.GetRolesAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(roles);
        return a;
    }

    private static ITokenService Tokens()
    {
        var t = Substitute.For<ITokenService>();
        t.IssueTokens(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(new TokenPair("access", "refresh",
                DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)));
        return t;
    }

    private SwitchRoleCommandHandler Sut(Guid? userId, IUserAdministration admin, ITokenService tokens) =>
        new(_db, CurrentUser(userId), admin, new ConsultantEligibilityService(_db, admin), tokens);

    private Guid SeedUser(
        string activeRole = "Student",
        AccountStatus status = AccountStatus.Active,
        DateTimeOffset? consultantVerifiedAt = null)
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Alaa",
            LastName = "Mostafa",
            Email = $"{id:N}@test.local",
            UserName = $"{id:N}@test.local",
            AccountStatus = status,
            ActiveRole = activeRole,
            Profile = new UserProfile { UserId = id, ConsultantVerifiedAt = consultantVerifiedAt },
        });
        _db.SaveChanges();
        return id;
    }

    private void SeedApprovedConsultantUpgrade(Guid userId)
    {
        _db.UpgradeRequests.Add(new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Approved,
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-2),
        });
        _db.SaveChanges();
    }

    [Fact]
    public async Task Student_without_consultant_role_cannot_switch_to_consultant()
    {
        var id = SeedUser();

        var act = () => Sut(id, Admin("Student"), Tokens())
            .Handle(new SwitchRoleCommand("Consultant"), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*do not hold*");
    }

    [Fact]
    public async Task Consultant_role_without_verification_cannot_switch_to_consultant()
    {
        // The reported bug: a student holds a Consultant role row but never had an
        // approved upgrade and carries no verification marker.
        var id = SeedUser(consultantVerifiedAt: null);

        var act = () => Sut(id, Admin("Student", "Consultant"), Tokens())
            .Handle(new SwitchRoleCommand("Consultant"), default);

        await act.Should().ThrowAsync<ForbiddenAccessException>()
            .WithMessage("*approved*");

        (await _db.Users.FirstAsync(u => u.Id == id)).ActiveRole.Should().Be("Student");
    }

    [Fact]
    public async Task Verified_consultant_can_switch_to_consultant()
    {
        var id = SeedUser(consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-20));
        var tokens = Tokens();

        var result = await Sut(id, Admin("Student", "Consultant"), tokens)
            .Handle(new SwitchRoleCommand("Consultant"), default);

        result.User.ActiveRole.Should().Be("Consultant");
        result.User.CanActAsConsultant.Should().BeTrue();
        (await _db.Users.FirstAsync(u => u.Id == id)).ActiveRole.Should().Be("Consultant");
        await tokens.Received().RevokeAllForUserAsync(id, Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Consultant_approved_via_upgrade_can_switch_to_consultant()
    {
        var id = SeedUser(consultantVerifiedAt: null);
        SeedApprovedConsultantUpgrade(id);

        var result = await Sut(id, Admin("Student", "Consultant"), Tokens())
            .Handle(new SwitchRoleCommand("Consultant"), default);

        result.User.ActiveRole.Should().Be("Consultant");
        result.User.CanActAsConsultant.Should().BeTrue();
    }

    [Fact]
    public async Task Switching_to_a_non_consultant_role_is_not_gated()
    {
        // A verified consultant switching back to Student must always work.
        var id = SeedUser(activeRole: "Consultant",
            consultantVerifiedAt: DateTimeOffset.UtcNow.AddDays(-20));

        var result = await Sut(id, Admin("Student", "Consultant"), Tokens())
            .Handle(new SwitchRoleCommand("Student"), default);

        result.User.ActiveRole.Should().Be("Student");
    }

    public void Dispose() => _db.Dispose();
}
