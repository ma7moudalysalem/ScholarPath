using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Queries.GetAiUsageSummary;

/// <summary>
/// Admin-only snapshot for the AI economy dashboard (PB-017 US-174..US-177).
/// Window is clamped to 7/30/90 days so a curious admin can't accidentally
/// trigger a full-table scan across all interaction history.
/// </summary>
public sealed record GetAiUsageSummaryQuery(int WindowDays = 30)
    : IRequest<AiUsageSummaryDto>;
