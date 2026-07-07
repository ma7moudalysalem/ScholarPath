using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;

/// <summary>
/// Single-row view of a paid ScholarshipProviderReview request. Shared by the Student and
/// ScholarshipProvider list/detail endpoints; the controller projection masks any
/// audience-specific fields the recipient must not see.
/// </summary>
public sealed record ScholarshipProviderReviewRequestDto
{
    public Guid Id { get; init; }
    public Guid ScholarshipId { get; init; }
    public string ScholarshipTitle { get; init; } = default!;

    public Guid StudentId { get; init; }
    public string? StudentName { get; init; }

    public Guid ScholarshipProviderId { get; init; }
    public string? ScholarshipProviderName { get; init; }

    public ScholarshipProviderReviewRequestStatus Status { get; init; }

    public decimal ReviewFeeUsdSnapshot { get; init; }
    public string Currency { get; init; } = "USD";

    /// <summary>True when the request is free (snapshot fee = 0, no payment row).</summary>
    public bool IsFree => ReviewFeeUsdSnapshot == 0m && PaymentId is null;

    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? AcceptedAt { get; init; }
    public DateTimeOffset? RejectedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public DateTimeOffset? ExpiredAt { get; init; }
    public DateTimeOffset? PendingExpiresAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? UpdatedAt { get; init; }

    public string? CancelReason { get; init; }
    public string? RejectReason { get; init; }

    /// <summary>Files the student attached for the provider to review (PB-005).</summary>
    public IReadOnlyList<ScholarshipProviderReviewRequestDocumentInfo> Documents { get; init; } = [];

    /// <summary>The provider's completeness feedback, shown to the student.</summary>
    public string? ProviderFeedback { get; init; }

    // ── Payment summary (denormalised for dashboard rendering) ──────────────
    public Guid? PaymentId { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public long AmountCents { get; init; }
    public long HeldAmountCents { get; init; }
    public long CapturedAmountCents { get; init; }
    public long RefundedAmountCents { get; init; }
    public long RetainedAmountCents { get; init; }
    public long PlatformCommissionCents { get; init; }
    public long ScholarshipProviderShareCents { get; init; }
    public string? PaymentReference { get; init; }
}

/// <summary>A file the student attached to a review/support request (PB-005).</summary>
public sealed record ScholarshipProviderReviewRequestDocumentInfo(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);
