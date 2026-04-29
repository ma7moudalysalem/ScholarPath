using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Common;

/// <summary>
/// Options source for the AI daily budget — Infrastructure binds it but the
/// gate lives in Application so handlers can share the same rule cheaply.
/// </summary>
public interface IAiCostOptions
{
    decimal DailyUserCostLimitUsd { get; }
}

/// <summary>
/// Daily rolling 24h cost ceiling per user. Called before every IAiService invocation.
/// </summary>
public sealed class AiCostGate(
    IApplicationDbContext db,
    IDateTimeService clock,
    IOptions<AiCostOptionsSnapshot> opts)
{
    public async Task EnsureWithinDailyBudgetAsync(Guid userId, decimal pendingCostUsd, CancellationToken ct)
    {
        var limit = opts.Value.DailyUserCostLimitUsd;
        if (limit <= 0m) return; // cap disabled

        var since = clock.UtcNow.AddHours(-24);

        var used = await db.AiInteractions
            .Where(i => i.UserId == userId && i.StartedAt >= since)
            .Select(i => (decimal?)i.CostUsd)
            .SumAsync(ct)
            .ConfigureAwait(false)
            ?? 0m;

        if (used + pendingCostUsd > limit)
        {
            throw new ConflictException(
                $"Daily AI budget exceeded (used ${used:0.###} + this call ${pendingCostUsd:0.###} > ${limit:0.###}). Try again tomorrow.");
        }
    }
}

/// <summary>
/// Bound by Infrastructure to the AiOptions section; exposed in Application
/// as a plain snapshot record to avoid dragging the full AiOptions type inward.
/// </summary>
public sealed class AiCostOptionsSnapshot : IAiCostOptions
{
    public decimal DailyUserCostLimitUsd { get; set; } = 1.00m;
    public int RecommendationTopN { get; set; } = 5;
}
