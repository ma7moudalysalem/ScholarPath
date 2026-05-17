using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using Xunit;

namespace ScholarPath.UnitTests.Chat;

/// <summary>
/// Covers <see cref="ChatContactReadService"/> — the direct-message compose
/// user-picker projection. Uses the EF Core in-memory provider so the Identity
/// join-tables (<c>UserRoles</c> / <c>Roles</c>) and the <c>UserBlock</c>
/// entities can be seeded together.
/// </summary>
public sealed class ChatContactReadServiceTests : IDisposable
{
    private readonly ApplicationDbContext _db;
    private readonly ChatContactReadService _service;

    private const int Limit = 20;

    private readonly Guid _studentRoleId = Guid.NewGuid();
    private readonly Guid _consultantRoleId = Guid.NewGuid();

    public ChatContactReadServiceTests()
    {
        _db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
        _service = new ChatContactReadService(_db);

        _db.Roles.Add(new ApplicationRole
        {
            Id = _studentRoleId, Name = "Student", NormalizedName = "STUDENT",
        });
        _db.Roles.Add(new ApplicationRole
        {
            Id = _consultantRoleId, Name = "Consultant", NormalizedName = "CONSULTANT",
        });
        _db.SaveChanges();
    }

    // ── Seed helpers ────────────────────────────────────────────────────────────

    private Guid SeedUser(
        string first = "Sarah",
        string last = "Adel",
        AccountStatus status = AccountStatus.Active,
        Guid? roleId = null,
        bool isDeleted = false)
    {
        var id = Guid.NewGuid();
        var email = $"u-{id:N}@test.com";
        _db.Users.Add(new ApplicationUser
        {
            Id = id,
            FirstName = first,
            LastName = last,
            Email = email,
            UserName = email,
            AccountStatus = status,
            IsDeleted = isDeleted,
        });

        if (roleId is not null)
        {
            _db.UserRoles.Add(new IdentityUserRole<Guid>
            {
                UserId = id,
                RoleId = roleId.Value,
            });
        }

        _db.SaveChanges();
        return id;
    }

    private void SeedBlock(Guid blockerId, Guid blockedUserId)
    {
        _db.UserBlocks.Add(new UserBlock
        {
            Id = Guid.NewGuid(),
            BlockerId = blockerId,
            BlockedUserId = blockedUserId,
        });
        _db.SaveChanges();
    }

    // ── Tests ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task SearchContacts_ExcludesTheCurrentUser()
    {
        var me = SeedUser(first: "Me");
        var other = SeedUser(first: "Other");

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(other);
    }

    [Fact]
    public async Task SearchContacts_ExcludesNonActiveUsers()
    {
        var me = SeedUser(first: "Me");
        SeedUser(first: "Suspended", status: AccountStatus.Suspended);
        SeedUser(first: "Pending", status: AccountStatus.PendingApproval);
        var active = SeedUser(first: "Active");

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(active);
    }

    [Fact]
    public async Task SearchContacts_ExcludesSoftDeletedUsers()
    {
        var me = SeedUser(first: "Me");
        SeedUser(first: "Deleted", isDeleted: true);

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchContacts_ExcludesUsersBlockedByTheCurrentUser()
    {
        var me = SeedUser(first: "Me");
        var blocked = SeedUser(first: "Blocked");
        SeedBlock(blockerId: me, blockedUserId: blocked);

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchContacts_ExcludesUsersWhoBlockedTheCurrentUser()
    {
        var me = SeedUser(first: "Me");
        var blocker = SeedUser(first: "Blocker");
        SeedBlock(blockerId: blocker, blockedUserId: me);

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchContacts_FiltersByNameSearchTerm()
    {
        var me = SeedUser(first: "Me");
        var ahmed = SeedUser(first: "Ahmed", last: "Hassan");
        SeedUser(first: "Sara", last: "Khalil");

        var result = await _service.SearchContactsAsync(me, "ahm", Limit, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(ahmed);
    }

    [Fact]
    public async Task SearchContacts_SearchTermMatchesAcrossFirstAndLastName()
    {
        var me = SeedUser(first: "Me");
        var target = SeedUser(first: "Ahmed", last: "Hassan");

        var result = await _service.SearchContactsAsync(me, "ahmed hass", Limit, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Id.Should().Be(target);
    }

    [Fact]
    public async Task SearchContacts_ProjectsNameAndRole()
    {
        var me = SeedUser(first: "Me");
        SeedUser(first: "Sarah", last: "Adel", roleId: _consultantRoleId);

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        var contact = result.Should().ContainSingle().Subject;
        contact.Name.Should().Be("Sarah Adel");
        contact.Role.Should().Be("Consultant");
    }

    [Fact]
    public async Task SearchContacts_RoleIsNull_WhenUserHasNoRole()
    {
        var me = SeedUser(first: "Me");
        SeedUser(first: "Roleless");

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Role.Should().BeNull();
    }

    [Fact]
    public async Task SearchContacts_RespectsTheResultLimit()
    {
        var me = SeedUser(first: "Me");
        for (var i = 0; i < 5; i++)
        {
            SeedUser(first: $"User{i:D2}", roleId: _studentRoleId);
        }

        var result = await _service.SearchContactsAsync(me, null, 3, CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchContacts_OrdersByName()
    {
        var me = SeedUser(first: "Me");
        SeedUser(first: "Charlie", last: "Zaki");
        SeedUser(first: "Alice", last: "Nour");
        SeedUser(first: "Bob", last: "Samy");

        var result = await _service.SearchContactsAsync(me, null, Limit, CancellationToken.None);

        result.Select(c => c.Name)
            .Should().ContainInOrder("Alice Nour", "Bob Samy", "Charlie Zaki");
    }

    public void Dispose() => _db.Dispose();
}
