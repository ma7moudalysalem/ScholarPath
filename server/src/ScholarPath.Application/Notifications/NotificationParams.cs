using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Notifications;

/// <summary>
/// Structured params the <see cref="INotificationCatalog"/> interpolates into
/// notification text (Task 5B). Every field is optional — a call site sets only
/// what its notification type needs.
/// </summary>
public sealed record NotificationParams
{
    /// <summary>Human-readable status, e.g. an application status.</summary>
    public string? StatusText { get; init; }

    /// <summary>A count — flag count, star rating, etc.</summary>
    public int? Count { get; init; }

    /// <summary>An entity title in English (e.g. a resource title).</summary>
    public string? TitleEn { get; init; }

    /// <summary>An entity title in Arabic.</summary>
    public string? TitleAr { get; init; }

    /// <summary>A free-text reason — rejection reason, dispute reason, etc.</summary>
    public string? Reason { get; init; }

    /// <summary>A pre-formatted money amount, e.g. "$45.00".</summary>
    public string? AmountText { get; init; }

    /// <summary>Refund variant for CompanyReviewRefunded: "Full", "Partial", or "Timeout".</summary>
    public string? RefundKind { get; init; }

    /// <summary>Pre-rendered content for an admin Broadcast — bypasses the catalog templates.</summary>
    public NotificationContent? RawContent { get; init; }

    public static readonly NotificationParams Empty = new();
}
