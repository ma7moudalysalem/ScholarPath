using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ApproveOnboarding;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Admin;

public class ReviewOnboardingRoleGrantTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Guid SeedPendingUser(ApplicationDbContext db, string requestedRole)
    {
        var id = Guid.NewGuid();
        db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Pending",
            LastName = "Payee",
            Email = $"{id:N}@scholarpath.local",
            UserName = $"{id:N}@scholarpath.local",
            AccountStatus = AccountStatus.PendingApproval,
            ActiveRole = requestedRole,
        });
        return id;
    }

    private static ReviewOnboardingCommandHandler Sut(
        ApplicationDbContext db, IUserAdministration admin) =>
        new(db, admin, Substitute.For<INotificationDispatcher>(),
            NullLogger<ReviewOnboardingCommandHandler>.Instance);

    [Fact]
    public async Task Approval_grants_the_requested_role_and_completes_onboarding()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "Company");
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        admin.SetAccountStatusAsync(userId, AccountStatus.Active,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Approve, null), default);

        await admin.Received().AddRoleAsync(userId, "Company", Arg.Any<CancellationToken>());
        (await db.Users.FirstAsync(u => u.Id == userId)).IsOnboardingComplete.Should().BeTrue();
    }

    [Fact]
    public async Task Rejection_does_not_grant_a_role()
    {
        using var db = CreateDb();
        var userId = SeedPendingUser(db, "Consultant");
        await db.SaveChangesAsync();

        var admin = Substitute.For<IUserAdministration>();
        // A rejected applicant returns to Unassigned so they can resubmit (FR-152).
        admin.SetAccountStatusAsync(userId, AccountStatus.Unassigned,
                Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(true);

        await Sut(db, admin).Handle(
            new ReviewOnboardingCommand(userId, OnboardingDecision.Reject, "Not verified"), default);

        await admin.DidNotReceive()
            .AddRoleAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
