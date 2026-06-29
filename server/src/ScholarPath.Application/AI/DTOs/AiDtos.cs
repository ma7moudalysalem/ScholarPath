using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Ai.DTOs;

// Cached shape — only AI-generated fields; stored as JSON in AiInteraction.ResponseText.
public sealed record RecommendationItemDto(
    Guid ScholarshipId,
    string TitleEn,
    string TitleAr,
    int MatchScore,
    string ExplanationEn,
    string ExplanationAr);

// API response shape — enriched with live scholarship metadata.
public sealed record RecommendationCardDto(
    Guid ScholarshipId,
    string TitleEn,
    string TitleAr,
    int MatchScore,
    string ExplanationEn,
    string ExplanationAr,
    DateTimeOffset Deadline,
    decimal? FundingAmountUsd,
    string FundingType);

public sealed record RecommendationsDto(
    IReadOnlyList<RecommendationCardDto> Items,
    string Disclaimer,
    DateTimeOffset GeneratedAt);

public sealed record EligibilityCriterionDto(
    string NameEn,
    string NameAr,
    string StudentValue,
    string ListingRequirement,
    string Match);

public sealed record EligibilityDto(
    Guid ScholarshipId,
    IReadOnlyList<EligibilityCriterionDto> Criteria,
    string SummaryEn,
    string SummaryAr,
    // SRS FR-117 — overall classification derived from the per-criterion verdicts.
    EligibilityVerdict Verdict,
    string Disclaimer,
    DateTimeOffset GeneratedAt);

public sealed record ChatAnswerDto(
    string SessionId,
    string Message,
    string Disclaimer,
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCostUsd,
    DateTimeOffset AnsweredAt,
    IReadOnlyList<ChatSourceDto> Sources);

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

public sealed record AiFeatureUsageDto(
    AiFeature Feature,
    int Interactions,
    decimal CostUsd,
    int? AvgLatencyMs);

public sealed record AiDailyCostPoint(DateOnly Date, decimal CostUsd);

public sealed record RecommendationCtrDto(
    int Impressions,
    int Clicks,
    decimal CtrPercent);

public sealed record AiUsageSummaryDto(
    int WindowDays,
    decimal TotalCostUsd,
    int TotalInteractions,
    IReadOnlyList<AiFeatureUsageDto> ByFeature,
    IReadOnlyList<AiDailyCostPoint> DailyCost,
    RecommendationCtrDto Recommendations,
    DateTimeOffset GeneratedAt);
