using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Admin;

/// <summary>
/// Approving a Consultant upgrade must grant the role AND stamp the official
/// verification marker (<see cref="UserProfile.ConsultantVerifiedAt"/>) so the
/// user passes consultant eligibility. Company upgrades leave it untouched.
/// </summary>
public sealed class ReviewUpgradeRequestConsultantMarkerTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly DateTimeOffset _now = new(2026, 7, 1, 10, 0, 0, TimeSpan.Zero);

    public ReviewUpgradeRequestConsultantMarkerTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
    }

    private ReviewUpgradeRequestCommandHandler Sut(IUserAdministration admin)
    {
        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(_now);
        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        return new ReviewUpgradeRequestCommandHandler(
            _db, admin, currentUser, clock,
            Substitute.For<INotificationDispatcher>(),
            NullLogger<ReviewUpgradeRequestCommandHandler>.Instance);
    }

    private Guid SeedUserWithProfile()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "Omar",
            LastName = "Khalil",
            Email = $"{id:N}@test.local",
            UserName = $"{id:N}@test.local",
            AccountStatus = AccountStatus.Active,
            Profile = new UserProfile { UserId = id },
        });
        _db.SaveChanges();
        return id;
    }

    private Guid SeedPendingRequest(Guid userId, UpgradeTarget target)
    {
        var reqId = Guid.NewGuid();
        _db.UpgradeRequests.Add(new UpgradeRequest
        {
            Id = reqId,
            UserId = userId,
            Target = target,
            Status = UpgradeRequestStatus.Pending,
            CreatedAt = _now.AddDays(-1),
        });
        _db.SaveChanges();
        return reqId;
    }

    [Fact]
    public async Task Approving_consultant_upgrade_sets_verification_marker()
    {
        var userId = SeedUserWithProfile();
        var reqId = SeedPendingRequest(userId, UpgradeTarget.Consultant);
        var admin = Substitute.For<IUserAdministration>();

        await Sut(admin).Handle(
            new ReviewUpgradeRequestCommand(reqId, UpgradeDecision.Approve, null), default);

        await admin.Received().AddRoleAsync(userId, "Consultant", Arg.Any<CancellationToken>());
        var profile = await _db.UserProfiles.FirstAsync(p => p.UserId == userId);
        profile.ConsultantVerifiedAt.Should().Be(_now);
    }

    [Fact]
    public async Task Approving_consultant_upgrade_creates_profile_if_missing()
    {
        var id = Guid.NewGuid();
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = "No",
            LastName = "Profile",
            Email = $"{id:N}@test.local",
            UserName = $"{id:N}@test.local",
            AccountStatus = AccountStatus.Active,
        });
        _db.SaveChanges();
        var reqId = SeedPendingRequest(id, UpgradeTarget.Consultant);

        await Sut(Substitute.For<IUserAdministration>()).Handle(
            new ReviewUpgradeRequestCommand(reqId, UpgradeDecision.Approve, null), default);

        var profile = await _db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == id);
        profile.Should().NotBeNull();
        profile!.ConsultantVerifiedAt.Should().Be(_now);
    }

    [Fact]
    public async Task Approving_company_upgrade_does_not_set_consultant_marker()
    {
        var userId = SeedUserWithProfile();
        var reqId = SeedPendingRequest(userId, UpgradeTarget.Company);

        await Sut(Substitute.For<IUserAdministration>()).Handle(
            new ReviewUpgradeRequestCommand(reqId, UpgradeDecision.Approve, null), default);

        var profile = await _db.UserProfiles.FirstAsync(p => p.UserId == userId);
        profile.ConsultantVerifiedAt.Should().BeNull();
    }

    [Fact]
    public async Task Rejecting_consultant_upgrade_does_not_set_marker()
    {
        var userId = SeedUserWithProfile();
        var reqId = SeedPendingRequest(userId, UpgradeTarget.Consultant);

        await Sut(Substitute.For<IUserAdministration>()).Handle(
            new ReviewUpgradeRequestCommand(reqId, UpgradeDecision.Reject, "Insufficient evidence"), default);

        var profile = await _db.UserProfiles.FirstAsync(p => p.UserId == userId);
        profile.ConsultantVerifiedAt.Should().BeNull();
    }

    public void Dispose() => _db.Dispose();
}
