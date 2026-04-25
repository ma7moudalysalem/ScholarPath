using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Queries.GetAiUsageSummary;

public sealed class GetAiUsageSummaryQueryHandler(
    IApplicationDbContext db,
    IDateTimeService clock)
    : IRequestHandler<GetAiUsageSummaryQuery, AiUsageSummaryDto>
{
    private static readonly int[] AllowedWindows = [7, 30, 90];

    public async Task<AiUsageSummaryDto> Handle(GetAiUsageSummaryQuery request, CancellationToken ct)
    {
        var window = AllowedWindows.Contains(request.WindowDays)
            ? request.WindowDays
            : 30;

        var now = clock.UtcNow;
        var since = now.AddDays(-window);

        // Per-feature aggregation. We keep latency provider-agnostic by pulling
        // the minimal (Feature, StartedAt, CompletedAt, CostUsd) projection and
        // folding in memory. Cardinality here is bounded by the window, which
        // the query clamps to ≤ 90 days — cheap enough.
        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.StartedAt >= since)
            .Select(i => new
            {
                i.Feature,
                i.StartedAt,
                i.CompletedAt,
                i.CostUsd,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byFeature = rows
            .GroupBy(r => r.Feature)
            .Select(g =>
            {
                var latencies = g
                    .Where(x => x.CompletedAt.HasValue)
                    .Select(x => (x.CompletedAt!.Value - x.StartedAt).TotalMilliseconds)
                    .ToList();
                var avg = latencies.Count == 0 ? (int?)null : (int)Math.Round(latencies.Average());
                return new AiFeatureUsageDto(
                    g.Key,
                    g.Count(),
                    g.Sum(x => x.CostUsd),
                    avg);
            })
            .OrderBy(x => x.Feature)
            .ToList();

        var totalInteractions = byFeature.Sum(x => x.Interactions);
        var totalCost = byFeature.Sum(x => x.CostUsd);

        // Daily cost series from the same in-memory rows (already bounded).
        var dailyCost = rows
            .GroupBy(r => DateOnly.FromDateTime(r.StartedAt.UtcDateTime))
            .OrderBy(g => g.Key)
            .Select(g => new AiDailyCostPoint(g.Key, g.Sum(x => x.CostUsd)))
            .ToList();

        // Recommendation CTR — impressions from the in-memory rows (already
        // pulled); clicks from the dedicated click-events table. Using
        // StartedAt + ClickedAt windows so a click on cached recs still counts
        // even when the generating interaction was slightly older.
        var impressions = rows.Count(r => r.Feature == AiFeature.Recommendation);

        var clicks = await db.RecommendationClickEvents
            .AsNoTracking()
            .CountAsync(e => e.ClickedAt >= since, ct)
            .ConfigureAwait(false);

        var ctrPercent = impressions == 0
            ? 0m
            : Math.Round((decimal)clicks / impressions * 100m, 2);

        return new AiUsageSummaryDto(
            WindowDays: window,
            TotalCostUsd: totalCost,
            TotalInteractions: totalInteractions,
            ByFeature: byFeature,
            DailyCost: dailyCost,
            Recommendations: new RecommendationCtrDto(impressions, clicks, ctrPercent),
            GeneratedAt: now);
    }
}
