using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Chat.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Read projection backing the direct-message compose user-picker. Lives in
/// Infrastructure because surfacing each user's role requires the Identity
/// join-tables (<c>AspNetUserRoles</c> / <c>AspNetRoles</c>), which are
/// deliberately kept off <see cref="IApplicationDbContext"/>. Mirrors the
/// pattern of <see cref="ConsultantReadService"/> and <see cref="AdminReadService"/>.
/// </summary>
public sealed class ChatContactReadService(ApplicationDbContext db) : IChatContactReadService
{
    public async Task<IReadOnlyList<ChatContactDto>> SearchContactsAsync(
        Guid currentUserId,
        string? query,
        int limit,
        CancellationToken ct)
    {
        // Users in a block relationship with the current user — in either
        // direction — cannot be messaged, so they are filtered out entirely.
        var blockedUserIds = await db.UserBlocks
            .AsNoTracking()
            .Where(b => b.BlockerId == currentUserId || b.BlockedUserId == currentUserId)
            .Select(b => b.BlockerId == currentUserId ? b.BlockedUserId : b.BlockerId)
            .Distinct()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Soft-deleted users are excluded by the global query filter on Users.
        var candidates = db.Users
            .AsNoTracking()
            .Where(u => u.Id != currentUserId
                        && u.AccountStatus == AccountStatus.Active
                        && !blockedUserIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            candidates = candidates.Where(u =>
                EF.Functions.Like(u.FirstName, $"%{term}%") ||
                EF.Functions.Like(u.LastName, $"%{term}%") ||
                EF.Functions.Like(u.FirstName + " " + u.LastName, $"%{term}%"));
        }

        var users = await candidates
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Take(limit)
            .Select(u => new
            {
                u.Id,
                u.FirstName,
                u.LastName,
                u.ProfileImageUrl,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (users.Count == 0)
        {
            return Array.Empty<ChatContactDto>();
        }

        // Batch-load roles for this page only — avoid an N+1 per contact.
        var ids = users.Select(u => u.Id).ToList();
        var roleMap = await (
                from ur in db.UserRoles
                join r in db.Roles on ur.RoleId equals r.Id
                where ids.Contains(ur.UserId) && r.Name != null
                select new { ur.UserId, r.Name })
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var roleByUser = roleMap
            .GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name!).OrderBy(n => n).First());

        return users
            .Select(u => new ChatContactDto(
                u.Id,
                $"{u.FirstName} {u.LastName}".Trim(),
                u.ProfileImageUrl,
                roleByUser.GetValueOrDefault(u.Id)))
            .ToList();
    }
}
