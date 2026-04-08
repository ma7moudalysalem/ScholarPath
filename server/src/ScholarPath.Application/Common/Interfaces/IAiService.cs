namespace ScholarPath.Application.Common.Interfaces;

public interface IAiService
{
    Task<AiRecommendationResult> GenerateRecommendationsAsync(
        Guid userId,
        int topN,
        CancellationToken ct);

    Task<AiEligibilityResult> CheckEligibilityAsync(
        Guid userId,
        Guid scholarshipId,
        CancellationToken ct);

    Task<AiChatResponse> AskAsync(
        Guid userId,
        string sessionId,
        string message,
        CancellationToken ct);
}

public sealed record AiRecommendationResult(
    IReadOnlyList<AiRecommendationItem> Items,
    string Disclaimer,
    int PromptTokens,
    int CompletionTokens);

public sealed record AiRecommendationItem(
    Guid ScholarshipId,
    int MatchScore, // 0..100
    string ExplanationEn,
    string ExplanationAr);

public sealed record AiEligibilityResult(
    IReadOnlyList<AiEligibilityCriterion> Criteria,
    string Summary,
    string Disclaimer);

public sealed record AiEligibilityCriterion(
    string Name,
    string StudentValue,
    string ListingRequirement,
    string Match); // "yes" | "partial" | "no" | "unknown"

public sealed record AiChatResponse(
    string Message,
    string Disclaimer,
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCostUsd);
