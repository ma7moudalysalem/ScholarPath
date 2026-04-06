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
