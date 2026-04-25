using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

public sealed class AdminReadService(
    ApplicationDbContext db,
    UserManager<ApplicationUser> users) : IAdminReadService
{
    public async Task<PagedResult<AdminUserRow>> SearchUsersAsync(
        string? search,
        AccountStatus? status,
        string? role,
        bool includeDeleted,
        int page,
        int pageSize,
        CancellationToken ct)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        // Base query — IgnoreQueryFilters so we can opt-in to deleted rows when the admin asks
        var q = db.Users.AsNoTracking();
        if (includeDeleted) q = q.IgnoreQueryFilters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(u =>
                EF.Functions.Like(u.Email!, $"%{s}%") ||
                EF.Functions.Like(u.FirstName, $"%{s}%") ||
                EF.Functions.Like(u.LastName, $"%{s}%"));
        }

        if (status.HasValue)
        {
            q = q.Where(u => u.AccountStatus == status.Value);
        }

        // Role filter — join via AspNetUserRoles → AspNetRoles
        if (!string.IsNullOrWhiteSpace(role))
        {
            var roleName = role.Trim();
            q = from u in q
                join ur in db.UserRoles on u.Id equals ur.UserId
                join r in db.Roles on ur.RoleId equals r.Id
                where r.Name == roleName
                select u;
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var pageUsers = await q
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.AccountStatus,
                u.IsOnboardingComplete,
                u.CreatedAt,
                u.LastLoginAt,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pageUsers.Count == 0)
        {
            return new PagedResult<AdminUserRow>(Array.Empty<AdminUserRow>(), page, pageSize, total);
        }

        // Batch-load roles for this page only (avoid N+1)
        var ids = pageUsers.Select(u => u.Id).ToList();
        var roleMap = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where ids.Contains(ur.UserId) && r.Name != null
            select new { ur.UserId, r.Name }
        )
        .AsNoTracking()
        .ToListAsync(ct)
        .ConfigureAwait(false);

        var rolesByUser = roleMap
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Name!).ToList());

        // Batch-load risk flags for this page (PB-018 FR-270). Missing row = not scored yet.
        var riskMap = await db.UserRiskFlags
            .AsNoTracking()
            .Where(f => ids.Contains(f.UserId))
            .Select(f => new { f.UserId, f.IsAtRisk, f.Score })
            .ToDictionaryAsync(x => x.UserId, x => (x.IsAtRisk, x.Score), ct)
            .ConfigureAwait(false);

        var rows = pageUsers.Select(u =>
        {
            var risk = riskMap.TryGetValue(u.Id, out var r) ? r : (IsAtRisk: false, Score: (decimal?)null);
            return new AdminUserRow(
                u.Id,
                u.Email ?? string.Empty,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.AccountStatus,
                u.IsOnboardingComplete,
                rolesByUser.TryGetValue(u.Id, out var rs) ? rs : Array.Empty<string>(),
                u.CreatedAt,
                u.LastLoginAt,
                risk.IsAtRisk,
                risk.Score);
        }).ToList();

        return new PagedResult<AdminUserRow>(rows, page, pageSize, total);
    }

    public async Task<AdminUserDetail?> GetUserDetailAsync(Guid userId, CancellationToken ct)
    {
        var user = await db.Users
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.FirstName,
                u.LastName,
                u.ProfileImageUrl,
                u.AccountStatus,
                u.IsOnboardingComplete,
                u.ActiveRole,
                u.CountryOfResidence,
                u.PreferredLanguage,
                u.CreatedAt,
                u.LastLoginAt,
                u.IsDeleted,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (user is null) return null;

        // Roles for a single user — fine to go through UserManager for correctness
        var appUser = await users.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        IReadOnlyList<string> roles = appUser is null
            ? Array.Empty<string>()
            : (await users.GetRolesAsync(appUser).ConfigureAwait(false)).ToArray();

        return new AdminUserDetail(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.ProfileImageUrl,
            user.AccountStatus,
            user.IsOnboardingComplete,
            roles,
            user.ActiveRole,
            user.CountryOfResidence,
            user.PreferredLanguage,
            user.CreatedAt,
            user.LastLoginAt,
            user.IsDeleted);
    }
}
