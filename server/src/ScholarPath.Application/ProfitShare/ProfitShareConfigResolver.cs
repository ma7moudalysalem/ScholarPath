using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ProfitShare;

/// <summary>
/// Resolves the profit-share percentage in force for a payment type (PB-014).
/// Centralises the "find the active <see cref="Domain.Entities.ProfitShareConfig"/>"
/// query that was previously duplicated — with subtly divergent filters — across
/// payment-intent creation and capture.
/// </summary>
public static class ProfitShareConfigResolver
{
    /// <summary>
    /// Returns the percentage of the <see cref="Domain.Entities.ProfitShareConfig"/>
    /// whose effective window contains the current instant, falling back to
    /// <see cref="ProfitShareCalculator.DefaultPercentage"/> when none is configured.
    /// </summary>
    public static async Task<decimal> ResolveActivePercentageAsync(
        IApplicationDbContext db, PaymentType type, CancellationToken ct)
    {
        // Snapshot 'now' once so both window bounds compare against a single
        // instant (the previous inline queries evaluated UtcNow twice).
        var now = DateTimeOffset.UtcNow;

        var config = await db.ProfitShareConfigs
            .AsNoTracking()
            .Where(c =>
                c.PaymentType == type &&
                c.EffectiveFrom <= now &&
                (c.EffectiveTo == null || c.EffectiveTo > now))
            .OrderByDescending(c => c.EffectiveFrom)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        return config?.Percentage ?? ProfitShareCalculator.DefaultPercentage(type);
    }
}
