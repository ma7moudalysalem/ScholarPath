using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Queries.GetMyRecommendations;

public sealed class GetMyRecommendationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<GetMyRecommendationsQueryHandler> logger)
    : IRequestHandler<GetMyRecommendationsQuery, RecommendationsDto?>
{
    private const string Disclaimer = "AI-generated guidance. Verify with official sources before acting.";

    public async Task<RecommendationsDto?> Handle(GetMyRecommendationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var maxAge = Math.Clamp(request.MaxAgeHours, 1, 24 * 14);
        var since = clock.UtcNow.AddHours(-maxAge);

        var row = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.UserId == userId
                && i.Feature == AiFeature.Recommendation
                && i.ErrorMessage == null
                && i.StartedAt >= since)
            .OrderByDescending(i => i.StartedAt)
            .Select(i => new { i.ResponseText, i.CompletedAt, i.StartedAt })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (row is null || string.IsNullOrWhiteSpace(row.ResponseText))
        {
            return null;
        }

        try
        {
            var cached = JsonSerializer.Deserialize<List<RecommendationItemDto>>(row.ResponseText)
                ?? new List<RecommendationItemDto>();

            // Re-hydrate live scholarship metadata (deadline/funding may change).
            var ids = cached.Select(i => i.ScholarshipId).ToList();
            var meta = await db.Scholarships
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .Select(s => new { s.Id, s.Deadline, s.FundingAmountUsd, s.FundingType })
                .ToDictionaryAsync(s => s.Id, ct)
                .ConfigureAwait(false);

            var cards = cached.Select(i =>
            {
                var m = meta.TryGetValue(i.ScholarshipId, out var x) ? x : null;
                return new RecommendationCardDto(
                    i.ScholarshipId, i.TitleEn, i.TitleAr,
                    i.MatchScore, i.ExplanationEn, i.ExplanationAr,
                    m?.Deadline ?? default, m?.FundingAmountUsd, m?.FundingType.ToString() ?? "");
            }).ToList();

            return new RecommendationsDto(cards, Disclaimer, row.CompletedAt ?? row.StartedAt);
        }
        catch (JsonException ex)
        {
            // If a row has corrupted JSON, treat it as cache miss so the client
            // can re-ask the command. Don't fail the whole query.
            logger.LogWarning(ex, "Skipping corrupted recommendations cache row for {UserId}", userId);
            return null;
        }
    }
}
