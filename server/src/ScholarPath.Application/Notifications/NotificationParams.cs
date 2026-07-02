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

    /// <summary>Refund variant for ScholarshipProviderReviewRefunded: "Full", "Partial", or "Timeout".</summary>
    public string? RefundKind { get; init; }

    /// <summary>Reminder variant for BookingReminder: "24h" or "1h" before the session starts.</summary>
    public string? ReminderKind { get; init; }

    /// <summary>A counterparty display name (consultant or student) for booking notifications.</summary>
    public string? CounterpartyName { get; init; }

    /// <summary>A pre-formatted UTC ISO timestamp for a booking's scheduled start, e.g. "2026-05-20 14:00 UTC".</summary>
    public string? StartAtText { get; init; }

    /// <summary>Pre-rendered content for an admin Broadcast — bypasses the catalog templates.</summary>
    public NotificationContent? RawContent { get; init; }

    /// <summary>
    /// A short text preview — used by chat notifications to show the first line
    /// of the message body in the notification title/body.
    /// </summary>
    public string? Preview { get; init; }

    // ── ScholarshipProviderReview request lifecycle (PB-005) ────────────────────────────
    // Pre-formatted, safe-to-render amounts and references for the paid
    // application-support flow. Every field is optional — a notification only
    // sets what it needs. None of these are PCI data: card numbers and full
    // bank accounts are NEVER passed through this record.

    /// <summary>Held amount, pre-formatted with currency (e.g. "$45.00"). 0 until capture.</summary>
    public string? HeldAmountText { get; init; }

    /// <summary>Captured amount, pre-formatted with currency. 0 while the payment is only on hold.</summary>
    public string? CapturedAmountText { get; init; }

    /// <summary>Refund amount, pre-formatted with currency. Null when no refund applies.</summary>
    public string? RefundAmountText { get; init; }

    /// <summary>Final retained (= captured – refunded) amount, pre-formatted with currency.</summary>
    public string? RetainedAmountText { get; init; }

    /// <summary>Platform commission share (10% of retained), pre-formatted with currency.</summary>
    public string? PlatformCommissionText { get; init; }

    /// <summary>ScholarshipProvider share (90% of retained), pre-formatted with currency.</summary>
    public string? ScholarshipProviderShareText { get; init; }

    /// <summary>Payment reference number for receipts and dashboards.</summary>
    public string? PaymentReference { get; init; }

    /// <summary>Scholarship name, English.</summary>
    public string? ScholarshipNameEn { get; init; }

    /// <summary>Scholarship name, Arabic.</summary>
    public string? ScholarshipNameAr { get; init; }

    /// <summary>Request status text (e.g. "Pending", "UnderReview").</summary>
    public string? RequestStatusText { get; init; }

    /// <summary>Payment status text (e.g. "Held", "Captured", "PartiallyRefunded", "Cancelled").</summary>
    public string? PaymentStatusText { get; init; }

    /// <summary>Transaction timestamp text, pre-formatted (e.g. "2026-05-22 14:30 UTC").</summary>
    public string? TransactionAtText { get; init; }

    public static readonly NotificationParams Empty = new();
}
