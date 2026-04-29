using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Commands.CheckEligibility;

public sealed class CheckEligibilityCommandHandler(
    IApplicationDbContext db,
    IAiService ai,
    AiCostGate gate,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<CheckEligibilityCommandHandler> logger)
    : IRequestHandler<CheckEligibilityCommand, EligibilityDto>
{
    private const decimal EstimatedCost = 0.0005m;

    public async Task<EligibilityDto> Handle(CheckEligibilityCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        await gate.EnsureWithinDailyBudgetAsync(userId, EstimatedCost, ct).ConfigureAwait(false);

        var interaction = new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Eligibility,
            Provider = AiProvider.Stub,
            ModelName = "local-match-v1",
            StartedAt = clock.UtcNow,
            PromptText = $"scholarshipId={request.ScholarshipId}",
            ResponseText = "",
            CreatedAt = clock.UtcNow,
        };
        db.AiInteractions.Add(interaction);

        try
        {
            var result = await ai.CheckEligibilityAsync(userId, request.ScholarshipId, ct).ConfigureAwait(false);

            var criteria = result.Criteria
                .Select(c => new EligibilityCriterionDto(c.Name, c.StudentValue, c.ListingRequirement, c.Match))
                .ToList();

            interaction.ResponseText = System.Text.Json.JsonSerializer.Serialize(new { result.Summary, criteria });
            interaction.CostUsd = EstimatedCost;
            interaction.CompletedAt = clock.UtcNow;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            return new EligibilityDto(
                request.ScholarshipId,
                criteria,
                result.Summary,
                result.Disclaimer,
                interaction.CompletedAt.Value);
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
