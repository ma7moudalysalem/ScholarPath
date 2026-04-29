using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Commands.GenerateRecommendations;

public sealed class GenerateRecommendationsCommandHandler(
    IApplicationDbContext db,
    IAiService ai,
    AiCostGate gate,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    IOptions<AiCostOptionsSnapshot> opts,
    ILogger<GenerateRecommendationsCommandHandler> logger)
    : IRequestHandler<GenerateRecommendationsCommand, RecommendationsDto>
{
    // Synthetic cost per item — mirrors LocalAiService internal constants so the
    // gate can estimate the charge before the call (OpenAI provider will use the
    // real per-token pricing table).
    private const decimal EstimatedCostPerItem = 0.0008m;

    public async Task<RecommendationsDto> Handle(GenerateRecommendationsCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var topN = request.TopN ?? opts.Value.RecommendationTopN;
        topN = Math.Clamp(topN, 1, 20);

        var estCost = EstimatedCostPerItem * topN;
        await gate.EnsureWithinDailyBudgetAsync(userId, estCost, ct).ConfigureAwait(false);

        var interaction = new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Recommendation,
            Provider = AiProvider.Stub,
            ModelName = "local-match-v1",
            StartedAt = clock.UtcNow,
            PromptText = $"topN={topN}",
            ResponseText = "",
            CreatedAt = clock.UtcNow,
        };
        db.AiInteractions.Add(interaction);

        try
        {
            var result = await ai.GenerateRecommendationsAsync(userId, topN, ct).ConfigureAwait(false);

            // Hydrate scholarship titles for the UI — AI returns only IDs + scores
            var ids = result.Items.Select(i => i.ScholarshipId).ToList();
            var scholarships = await db.Scholarships
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .Select(s => new { s.Id, s.TitleEn, s.TitleAr })
                .ToListAsync(ct)
                .ConfigureAwait(false);
            var titleMap = scholarships.ToDictionary(s => s.Id, s => (s.TitleEn, s.TitleAr));

            var items = result.Items.Select(i =>
            {
                var t = titleMap.TryGetValue(i.ScholarshipId, out var pair)
                    ? pair
                    : (TitleEn: "", TitleAr: "");
                return new RecommendationItemDto(
                    i.ScholarshipId, t.TitleEn, t.TitleAr,
                    i.MatchScore, i.ExplanationEn, i.ExplanationAr);
            }).ToList();

            interaction.ResponseText = System.Text.Json.JsonSerializer.Serialize(items);
            interaction.PromptTokens = result.PromptTokens;
            interaction.CompletionTokens = result.CompletionTokens;
            interaction.CostUsd = EstimatedCostPerItem * Math.Max(items.Count, 1);
            interaction.CompletedAt = clock.UtcNow;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new RecommendationsDto(items, result.Disclaimer, interaction.CompletedAt.Value);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            interaction.ErrorMessage = ex.Message;
            interaction.CompletedAt = clock.UtcNow;
            try { await db.SaveChangesAsync(ct).ConfigureAwait(false); }
            catch (Exception saveEx) { logger.LogError(saveEx, "Failed to persist failed AiInteraction."); }
            throw;
        }
    }
}
