using System.Linq.Expressions;
using ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Common;

/// <summary>
/// One canonical projection from <see cref="ScholarshipProviderReviewRequest"/> (joined to
/// its <see cref="Scholarship"/>, <see cref="ApplicationUser"/>s, and paired
/// <see cref="Payment"/>) to <see cref="ScholarshipProviderReviewRequestDto"/>. Used by
/// every list/detail query so dashboards always see the same money fields.
/// </summary>
public static class ScholarshipProviderReviewRequestMapper
{
    public static Expression<Func<ScholarshipProviderReviewRequest, ScholarshipProviderReviewRequestDto>> Projection => r =>
        new ScholarshipProviderReviewRequestDto
        {
            Id = r.Id,
            ScholarshipId = r.ScholarshipId,
            ScholarshipTitle = r.Scholarship != null
                ? (r.Scholarship.TitleEn ?? r.Scholarship.TitleAr ?? "")
                : string.Empty,

            StudentId = r.StudentId,
            StudentName = r.Student != null
                ? (r.Student.FirstName + " " + r.Student.LastName).Trim()
                : null,

            ScholarshipProviderId = r.ScholarshipProviderId,
            ScholarshipProviderName = r.ScholarshipProvider != null
                ? (r.ScholarshipProvider.FirstName + " " + r.ScholarshipProvider.LastName).Trim()
                : null,

            Status = r.Status,
            ReviewFeeUsdSnapshot = r.ReviewFeeUsdSnapshot,
            Currency = r.Currency,

            SubmittedAt = r.SubmittedAt,
            AcceptedAt = r.AcceptedAt,
            RejectedAt = r.RejectedAt,
            CompletedAt = r.CompletedAt,
            CancelledAt = r.CancelledAt,
            ExpiredAt = r.ExpiredAt,
            PendingExpiresAt = r.PendingExpiresAt,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt,

            CancelReason = r.CancelReason,
            RejectReason = r.RejectReason,

            PaymentId = r.PaymentId,
            PaymentStatus = r.Payment != null ? r.Payment.Status : (PaymentStatus?)null,
            AmountCents = r.Payment != null ? r.Payment.AmountCents : 0,
            HeldAmountCents = r.Payment != null && r.Payment.Status == PaymentStatus.Held
                ? r.Payment.AmountCents : 0,
            CapturedAmountCents = r.Payment != null
                && (r.Payment.Status == PaymentStatus.Captured
                    || r.Payment.Status == PaymentStatus.PartiallyRefunded
                    || r.Payment.Status == PaymentStatus.Refunded)
                ? r.Payment.AmountCents : 0,
            RefundedAmountCents = r.Payment != null ? r.Payment.RefundedAmountCents : 0,
            RetainedAmountCents = r.Payment != null
                ? r.Payment.AmountCents - r.Payment.RefundedAmountCents
                : 0,
            PlatformCommissionCents = r.Payment != null ? r.Payment.ProfitShareAmountCents : 0,
            ScholarshipProviderShareCents = r.Payment != null ? r.Payment.PayeeAmountCents : 0,
            PaymentReference = r.Payment != null ? r.Payment.StripePaymentIntentId : null,
        };
}
