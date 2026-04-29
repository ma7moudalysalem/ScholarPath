using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.DTOs;

public sealed record RecommendationItemDto(
    Guid ScholarshipId,
    string TitleEn,
    string TitleAr,
    int MatchScore,
    string ExplanationEn,
    string ExplanationAr);

public sealed record RecommendationsDto(
    IReadOnlyList<RecommendationItemDto> Items,
    string Disclaimer,
    DateTimeOffset GeneratedAt);

public sealed record EligibilityCriterionDto(
    string Name,
    string StudentValue,
    string ListingRequirement,
    string Match);

public sealed record EligibilityDto(
    Guid ScholarshipId,
    IReadOnlyList<EligibilityCriterionDto> Criteria,
    string Summary,
    string Disclaimer,
    DateTimeOffset GeneratedAt);

public sealed record ChatAnswerDto(
    string SessionId,
    string Message,
    string Disclaimer,
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCostUsd,
    DateTimeOffset AnsweredAt);

public sealed record AiInteractionRowDto(
    Guid Id,
    AiFeature Feature,
    string? ModelName,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    int PromptTokens,
    int CompletionTokens,
    decimal CostUsd,
    bool Succeeded);
