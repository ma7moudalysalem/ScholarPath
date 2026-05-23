using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviewRequests.DTOs;

/// <summary>
/// Single-row view of a paid CompanyReview request. Shared by the Student and
/// Company list/detail endpoints; the controller projection masks any
/// audience-specific fields the recipient must not see.
/// </summary>
public sealed record CompanyReviewRequestDto
{
    public Guid Id { get; init; }
    public Guid ScholarshipId { get; init; }
    public string ScholarshipTitle { get; init; } = default!;

    public Guid StudentId { get; init; }
    public string? StudentName { get; init; }

    public Guid CompanyId { get; init; }
    public string? CompanyName { get; init; }

    public CompanyReviewRequestStatus Status { get; init; }

    public decimal ReviewFeeUsdSnapshot { get; init; }
    public string Currency { get; init; } = "USD";

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

    // ── Payment summary (denormalised for dashboard rendering) ──────────────
    public Guid? PaymentId { get; init; }
    public PaymentStatus? PaymentStatus { get; init; }
    public long AmountCents { get; init; }
    public long HeldAmountCents { get; init; }
    public long CapturedAmountCents { get; init; }
    public long RefundedAmountCents { get; init; }
    public long RetainedAmountCents { get; init; }
    public long PlatformCommissionCents { get; init; }
    public long CompanyShareCents { get; init; }
    public string? PaymentReference { get; init; }
}
