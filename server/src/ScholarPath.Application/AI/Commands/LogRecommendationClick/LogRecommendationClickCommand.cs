using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.LogRecommendationClick;

/// <summary>
/// Records that a student opened a scholarship surfaced by the recommendation
/// engine. Backs the CTR widget on the AI-economy dashboard (PB-017 / FR-249).
/// </summary>
[Auditable(AuditAction.Create, "RecommendationClick",
    SummaryTemplate = "Recommendation click logged ({ScholarshipId})",
    TargetIdProperty = "EventId",
    SkipOnNull = true)]
public sealed record LogRecommendationClickCommand(
    Guid ScholarshipId,
    Guid? AiInteractionId,
    string? Source) : IRequest<LogRecommendationClickResult?>;

public sealed record LogRecommendationClickResult(Guid EventId, bool Deduplicated);
