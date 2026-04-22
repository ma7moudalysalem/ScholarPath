using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

public sealed class UserAdministration(
    UserManager<ApplicationUser> users,
    ApplicationDbContext db,
    IDateTimeService clock,
    ILogger<UserAdministration> logger) : IUserAdministration
{
    public async Task<bool> SetAccountStatusAsync(Guid userId, AccountStatus status, string? reason, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;

        user.AccountStatus = status;
        user.UpdatedAt = clock.UtcNow;
        var result = await users.UpdateAsync(user).ConfigureAwait(false);

        if (status == AccountStatus.Suspended || status == AccountStatus.Deactivated)
        {
            await RevokeAllSessionsAsync(userId, reason ?? $"Status changed to {status}", ct).ConfigureAwait(false);
        }

        if (!result.Succeeded)
        {
            logger.LogWarning("SetAccountStatus failed for {UserId}: {Errors}",
                userId, string.Join(";", result.Errors.Select(e => e.Description)));
        }

        return result.Succeeded;
    }

    public async Task<bool> SoftDeleteAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null || user.IsDeleted) return false;

        var now = clock.UtcNow;
        user.IsDeleted = true;
        user.DeletedAt = now;
        user.AccountStatus = AccountStatus.Deactivated;
        user.UpdatedAt = now;
        await users.UpdateAsync(user).ConfigureAwait(false);
        await RevokeAllSessionsAsync(userId, "Account deleted by admin", ct).ConfigureAwait(false);
        return true;
    }

    public async Task<IReadOnlyList<string>> GetRolesAsync(Guid userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return Array.Empty<string>();
        var list = await users.GetRolesAsync(user).ConfigureAwait(false);
        return list.ToArray();
    }

    public async Task<bool> AddRoleAsync(Guid userId, string role, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;
        if (await users.IsInRoleAsync(user, role).ConfigureAwait(false)) return true;
        var result = await users.AddToRoleAsync(user, role).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task<bool> RemoveRoleAsync(Guid userId, string role, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return false;
        if (!await users.IsInRoleAsync(user, role).ConfigureAwait(false)) return true;
        var result = await users.RemoveFromRoleAsync(user, role).ConfigureAwait(false);
        return result.Succeeded;
    }

    public async Task RevokeAllSessionsAsync(Guid userId, string reason, CancellationToken ct)
    {
        var tokens = await db.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct).ConfigureAwait(false);

        if (tokens.Count == 0) return;

        var now = clock.UtcNow;
        foreach (var t in tokens)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            t.RevokedReason = reason;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
