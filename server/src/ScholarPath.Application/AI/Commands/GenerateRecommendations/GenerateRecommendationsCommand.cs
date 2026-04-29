using MediatR;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.Commands.GenerateRecommendations;

[Auditable(AuditAction.Create, "AiInteraction",
    SummaryTemplate = "AI recommendations generated (topN={TopN})")]
public sealed record GenerateRecommendationsCommand(int? TopN = null)
    : IRequest<RecommendationsDto>;
