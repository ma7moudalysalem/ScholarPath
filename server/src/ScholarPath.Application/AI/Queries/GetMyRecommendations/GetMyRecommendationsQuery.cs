using MediatR;
using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Ai.Queries.GetMyRecommendations;

/// <summary>
/// Reads the user's most recent successful Recommendations interaction from
/// AiInteractions (effectively a per-user cache) without re-running the
/// provider. Returns null when the user has no recommendations cached —
/// the client is expected to fall back to the POST command in that case.
/// </summary>
public sealed record GetMyRecommendationsQuery(int MaxAgeHours = 24)
    : IRequest<RecommendationsDto?>;
