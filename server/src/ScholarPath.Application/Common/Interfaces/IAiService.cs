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
    string Disclaimer,
    EligibilityVerdict Verdict);

public sealed record AiEligibilityCriterion(
    string Name,
    string StudentValue,
    string ListingRequirement,
    string Match); // "yes" | "partial" | "no" | "unknown"

/// <summary>
/// SRS FR-117 — the mandated overall eligibility classification, derived from
/// the per-criterion verdicts.
/// </summary>
public enum EligibilityVerdict
{
    /// <summary>All criteria are met (or "any"/unknown-but-no-failures).</summary>
    Eligible = 0,

    /// <summary>Some criteria are met or partially met, but none fail outright.</summary>
    PartiallyEligible = 1,

    /// <summary>At least one criterion fails outright.</summary>
    NotEligible = 2,
}

public sealed record AiChatResponse(
    string Message,
    string Disclaimer,
    int PromptTokens,
    int CompletionTokens,
    decimal EstimatedCostUsd,
    IReadOnlyList<ChatSource> Sources);

/// <summary>
/// A knowledge-base document the RAG retriever surfaced as grounding context
/// for a chat answer — shown to the user as a citation.
/// </summary>
public sealed record ChatSource(
    string Title,
    string SourceType,            // "Scholarship" | "Faq"
    Guid? ScholarshipId,
    double Score);
