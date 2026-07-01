using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Services;

/// <summary>
/// Default <see cref="IConsultantEligibilityService"/> implementation. Reads the
/// user's roles through <see cref="IUserAdministration"/> (the Identity
/// join-tables are not exposed on <see cref="IApplicationDbContext"/>) and the
/// approval signals through the application DbContext.
/// </summary>
public sealed class ConsultantEligibilityService(
    IApplicationDbContext db,
    IUserAdministration userAdministration) : IConsultantEligibilityService
{
    public const string ConsultantRole = "Consultant";

    public async Task<bool> CanActAsConsultantAsync(Guid userId, CancellationToken ct)
    {
        var roles = await userAdministration.GetRolesAsync(userId, ct).ConfigureAwait(false);
        return await CanActAsConsultantAsync(userId, roles, ct).ConfigureAwait(false);
    }

    public async Task<bool> CanActAsConsultantAsync(
        Guid userId, IReadOnlyList<string> roles, CancellationToken ct)
    {
        // Rule 1 — must actually hold the Consultant role. Cheapest check first;
        // it short-circuits every non-consultant without touching the database.
        if (!roles.Contains(ConsultantRole, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Rule 2 + first half of Rule 3 — account must be Active and we grab the
        // verification marker in the same round-trip.
        var snapshot = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.AccountStatus,
                ConsultantVerifiedAt = u.Profile != null ? u.Profile.ConsultantVerifiedAt : null,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (snapshot is null || snapshot.AccountStatus != AccountStatus.Active)
        {
            return false;
        }

        // Rule 3a — the official verification marker (set by the seeders and by
        // both admin approval paths). This is the canonical signal.
        if (snapshot.ConsultantVerifiedAt is not null)
        {
            return true;
        }

        // Rule 3b — fallback for accounts approved before the marker was written:
        // an approved, non-deleted Consultant upgrade request is an equally valid
        // approval signal, so historical data keeps working without a manual
        // backfill.
        return await db.UpgradeRequests
            .AsNoTracking()
            .AnyAsync(r => r.UserId == userId
                        && r.Target == UpgradeTarget.Consultant
                        && r.Status == UpgradeRequestStatus.Approved
                        && !r.IsDeleted, ct)
            .ConfigureAwait(false);
    }
}
