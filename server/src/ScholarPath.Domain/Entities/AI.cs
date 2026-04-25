using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class AiInteraction : AuditableEntity
{
    public Guid UserId { get; set; }
    public AiFeature Feature { get; set; }
    public AiProvider Provider { get; set; }
    public string? ModelName { get; set; }
    public string? SessionId { get; set; }

    public string PromptText { get; set; } = default!;
    public string ResponseText { get; set; } = default!;
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public decimal CostUsd { get; set; }

    public string? MetadataJson { get; set; } // e.g., recommendation scores, eligibility breakdown
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Per-click record for the Recommendation CTR metric (PB-017 US-176 / FR-249).
/// Source is the card / list / modal from which the user clicked a recommendation.
/// AiInteractionId is nullable because the user can click a recommendation from
/// a cached batch where no active AI call happened this session.
/// </summary>
public class RecommendationClickEvent : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid ScholarshipId { get; set; }
    public Guid? AiInteractionId { get; set; }
    public DateTimeOffset ClickedAt { get; set; } = DateTimeOffset.UtcNow;
    public string Source { get; set; } = "card"; // card | list | modal

    public ApplicationUser? User { get; set; }
    public Scholarship? Scholarship { get; set; }
    public AiInteraction? AiInteraction { get; set; }
}

/// <summary>
/// One-row-per-prompt sample selected monthly by <c>RedactionAuditSamplingJob</c>
/// for human-reviewed PII-redaction compliance (PB-017 US-178 / FR-254..FR-256).
/// Verdict is null until a reviewer submits their assessment.
/// </summary>
public class AiRedactionAuditSample : BaseEntity
{
    public Guid AiInteractionId { get; set; }
    public Guid UserId { get; set; }
    public string RedactedPrompt { get; set; } = default!;
    public DateTimeOffset SampledAt { get; set; } = DateTimeOffset.UtcNow;
    public RedactionVerdict? Verdict { get; set; }
    public Guid? ReviewerUserId { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }

    public AiInteraction? AiInteraction { get; set; }
    public ApplicationUser? User { get; set; }
    public ApplicationUser? Reviewer { get; set; }
}
