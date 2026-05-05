using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;

namespace ScholarPath.UnitTests.Admin;

/// <summary>
/// Integration-ish coverage for the PB-011 T-022 rule:
/// "suspend user → they get logged out immediately on next request"
/// which we enforce by revoking every active refresh token when the status
/// flips to Suspended or Deactivated.
/// </summary>
public class UserAdministrationTests
{
    private static (ApplicationDbContext db, UserAdministration admin, IDateTimeService clock) BuildSut()
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new ApplicationDbContext(opts);

        // UserManager needs a user-store + a pile of optional services. We only
        // exercise FindByIdAsync + UpdateAsync, which the default UserStore
        // (backed by our DbContext) covers. Both the manager and the store hold
        // a ref to the DbContext; we dispose the context in the test's finalizer
        // so leaking the manager here is benign for unit-test scope.
#pragma warning disable CA2000 // Dispose objects before losing scope
        var store = new UserStore<ApplicationUser, ApplicationRole, ApplicationDbContext, Guid>(db);
        var userManager = new UserManager<ApplicationUser>(
            store,
            optionsAccessor: null!,
            passwordHasher: new PasswordHasher<ApplicationUser>(),
            userValidators: Array.Empty<IUserValidator<ApplicationUser>>(),
            passwordValidators: Array.Empty<IPasswordValidator<ApplicationUser>>(),
            keyNormalizer: new UpperInvariantLookupNormalizer(),
            errors: new IdentityErrorDescriber(),
            services: null!,
            logger: NullLogger<UserManager<ApplicationUser>>.Instance);
#pragma warning restore CA2000

        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);

        var admin = new UserAdministration(
            userManager, db, clock,
            NullLogger<UserAdministration>.Instance);

        return (db, admin, clock);
    }

    private static async Task<Guid> SeedActiveUserWithTokensAsync(ApplicationDbContext db, int tokenCount)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = "victim@example.com",
            NormalizedUserName = "VICTIM@EXAMPLE.COM",
            Email = "victim@example.com",
            NormalizedEmail = "VICTIM@EXAMPLE.COM",
            FirstName = "V", LastName = "User",
            AccountStatus = AccountStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
        };
        db.Users.Add(user);

        for (var i = 0; i < tokenCount; i++)
        {
            db.RefreshTokens.Add(new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = $"hash-{i}",
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7),
                IsRevoked = false,
                CreatedAt = DateTimeOffset.UtcNow,
            });
        }
        await db.SaveChangesAsync();
        return user.Id;
    }

    [Fact]
    public async Task Suspend_revokes_every_active_refresh_token()
    {
        var (db, admin, _) = BuildSut();
        var uid = await SeedActiveUserWithTokensAsync(db, tokenCount: 3);

        var ok = await admin.SetAccountStatusAsync(uid, AccountStatus.Suspended, "policy violation", default);

        ok.Should().BeTrue();
        (await db.Users.FindAsync(uid))!.AccountStatus.Should().Be(AccountStatus.Suspended);
        var tokens = await db.RefreshTokens.Where(t => t.UserId == uid).ToListAsync();
        tokens.Should().HaveCount(3);
        tokens.Should().OnlyContain(t => t.IsRevoked && t.RevokedReason == "policy violation");
    }

    [Fact]
    public async Task Deactivate_also_revokes_tokens()
    {
        var (db, admin, _) = BuildSut();
        var uid = await SeedActiveUserWithTokensAsync(db, tokenCount: 2);

        await admin.SetAccountStatusAsync(uid, AccountStatus.Deactivated, null, default);

        var tokens = await db.RefreshTokens.Where(t => t.UserId == uid).ToListAsync();
        tokens.Should().OnlyContain(t => t.IsRevoked);
    }

    [Fact]
    public async Task Activate_does_not_revoke_tokens()
    {
        var (db, admin, _) = BuildSut();
        var uid = await SeedActiveUserWithTokensAsync(db, tokenCount: 2);

        await admin.SetAccountStatusAsync(uid, AccountStatus.Active, null, default);

        var tokens = await db.RefreshTokens.Where(t => t.UserId == uid).ToListAsync();
        tokens.Should().OnlyContain(t => !t.IsRevoked);
    }

    [Fact]
    public async Task SoftDelete_sets_flags_and_revokes_everything()
    {
        var (db, admin, _) = BuildSut();
        var uid = await SeedActiveUserWithTokensAsync(db, tokenCount: 2);

        var ok = await admin.SoftDeleteAsync(uid, default);

        ok.Should().BeTrue();
        var user = (await db.Users.FindAsync(uid))!;
        user.IsDeleted.Should().BeTrue();
        user.DeletedAt.Should().NotBeNull();
        user.AccountStatus.Should().Be(AccountStatus.Deactivated);

        var tokens = await db.RefreshTokens.Where(t => t.UserId == uid).ToListAsync();
        tokens.Should().OnlyContain(t => t.IsRevoked && t.RevokedReason == "Account deleted by admin");
    }

    [Fact]
    public async Task Unknown_user_returns_false_without_side_effects()
    {
        var (_, admin, _) = BuildSut();
        var ok = await admin.SetAccountStatusAsync(Guid.NewGuid(), AccountStatus.Active, null, default);
        ok.Should().BeFalse();
    }
}
