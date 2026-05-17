using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Auth.Commands.SelectRole;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Auth;

public class SelectRoleCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ICurrentUserService CurrentUser(Guid id)
    {
        var u = Substitute.For<ICurrentUserService>();
        u.UserId.Returns(id);
        return u;
    }

    private static ITokenService TokenService()
    {
        var ts = Substitute.For<ITokenService>();
        ts.IssueTokens(Arg.Any<ApplicationUser>(), Arg.Any<IEnumerable<string>>(),
                Arg.Any<string?>(), Arg.Any<bool>())
            .Returns(new TokenPair("access", "refresh",
                DateTimeOffset.UtcNow.AddHours(1), DateTimeOffset.UtcNow.AddDays(7)));
        return ts;
    }

    private static SelectRoleCommandHandler Sut(
        ApplicationDbContext db, Guid userId, IUserAdministration admin) =>
        new(db, CurrentUser(userId), admin, TokenService(),
            NullLogger<SelectRoleCommandHandler>.Instance);

    private static Guid SeedUnassignedUser(ApplicationDbContext db)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "New",
            LastName = "User",
            Email = "new@scholarpath.local",
            UserName = "new@scholarpath.local",
            AccountStatus = AccountStatus.Unassigned,
        });
        return id;
    }

    [Fact]
    public async Task Student_selection_activates_account_immediately()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        await Sut(db, userId, admin).Handle(new SelectRoleCommand("Student"), default);

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.AccountStatus.Should().Be(AccountStatus.Active);
        user.ActiveRole.Should().Be("Student");
        user.IsOnboardingComplete.Should().BeTrue();
        await admin.Received().AddRoleAsync(userId, "Student", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Company_selection_enters_the_onboarding_queue()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(Array.Empty<string>());

        await Sut(db, userId, admin).Handle(new SelectRoleCommand("Company"), default);

        var user = await db.Users.FirstAsync(u => u.Id == userId);
        user.AccountStatus.Should().Be(AccountStatus.PendingApproval);
        user.ActiveRole.Should().Be("Company");
        user.IsOnboardingComplete.Should().BeFalse();
        await admin.DidNotReceive()
            .AddRoleAsync(userId, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejects_a_second_role_selection()
    {
        using var db = CreateDb();
        var userId = SeedUnassignedUser(db);
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.GetRolesAsync(userId, Arg.Any<CancellationToken>()).Returns(new[] { "Student" });

        var act = () => Sut(db, userId, admin).Handle(new SelectRoleCommand("Company"), default);

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public void Validator_rejects_unknown_role()
    {
        var v = new SelectRoleCommandValidator();

        v.Validate(new SelectRoleCommand("Admin")).IsValid.Should().BeFalse();
        v.Validate(new SelectRoleCommand("")).IsValid.Should().BeFalse();
        v.Validate(new SelectRoleCommand("Student")).IsValid.Should().BeTrue();
    }
}
