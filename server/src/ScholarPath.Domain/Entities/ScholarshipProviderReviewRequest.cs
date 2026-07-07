using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A paid application-support / document-review request raised by a Student
/// against a ScholarshipProvider-owned scholarship (PB-005 "Apply Now" flow).
///
/// Pairs 1:1 with a <see cref="Payment"/> row of type
/// <see cref="PaymentType.ScholarshipProviderReview"/>: the request drives the lifecycle
/// (Draft → Submitted → Pending → UnderReview → Completed → Closed plus the
/// cancel / reject / expire branches), the Payment row carries the money.
///
/// The <see cref="ReviewFeeUsdSnapshot"/> column is a point-in-time copy of
/// <see cref="Scholarship.ReviewFeeUsd"/> taken at submission. If the ScholarshipProvider
/// later edits the scholarship's review fee, in-flight requests still settle
/// at the price the Student saw and authorised.
/// </summary>
public class ScholarshipProviderReviewRequest : AuditableEntity, ISoftDeletable
{
    public Guid StudentId { get; set; }
    public Guid ScholarshipProviderId { get; set; }
    public Guid ScholarshipId { get; set; }

    /// <summary>
    /// Optional link to the catalog-tracking <see cref="ApplicationTracker"/>
    /// row, so the company can see "this paid review request is for that
    /// application". Optional because Apply Now creates the request before the
    /// Application form is filled in.
    /// </summary>
    public Guid? ApplicationTrackerId { get; set; }

    /// <summary>
    /// FK to the underlying <see cref="Payment"/> row. Null only in the brief
    /// window between Draft and Submitted (no payment intent has been created
    /// yet); always set once <see cref="ScholarshipProviderReviewRequestStatus.Submitted"/>.
    /// </summary>
    public Guid? PaymentId { get; set; }

    public ScholarshipProviderReviewRequestStatus Status { get; set; }
        = ScholarshipProviderReviewRequestStatus.Draft;

    /// <summary>Snapshot of <see cref="Scholarship.ReviewFeeUsd"/> at submission time.</summary>
    public decimal ReviewFeeUsdSnapshot { get; set; }

    /// <summary>Currency used at submission — defaults to USD until multi-currency lands.</summary>
    public string Currency { get; set; } = "USD";

    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }

    /// <summary>
    /// Deadline after which a still-Pending request auto-expires (cancel hold,
    /// no charge). Defaults to 7 days from <see cref="SubmittedAt"/>.
    /// </summary>
    public DateTimeOffset? PendingExpiresAt { get; set; }

    public string? CancelReason { get; set; }
    public string? RejectReason { get; set; }

    /// <summary>
    /// The provider's completeness feedback on the student's attached documents
    /// (PB-005) — what's missing / what to fix before applying. Set when the
    /// provider marks the request Completed; visible to the student.
    /// </summary>
    public string? ProviderFeedback { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Student { get; set; }
    public ApplicationUser? ScholarshipProvider { get; set; }
    public Scholarship? Scholarship { get; set; }
    public Payment? Payment { get; set; }
}
