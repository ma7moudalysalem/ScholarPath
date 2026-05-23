using System.Linq.Expressions;
using ScholarPath.Application.CompanyReviewRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviewRequests.Common;

/// <summary>
/// One canonical projection from <see cref="CompanyReviewRequest"/> (joined to
/// its <see cref="Scholarship"/>, <see cref="ApplicationUser"/>s, and paired
/// <see cref="Payment"/>) to <see cref="CompanyReviewRequestDto"/>. Used by
/// every list/detail query so dashboards always see the same money fields.
/// </summary>
public static class CompanyReviewRequestMapper
{
    public static Expression<Func<CompanyReviewRequest, CompanyReviewRequestDto>> Projection => r =>
        new CompanyReviewRequestDto
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

            CompanyId = r.CompanyId,
            CompanyName = r.Company != null
                ? (r.Company.FirstName + " " + r.Company.LastName).Trim()
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
            CompanyShareCents = r.Payment != null ? r.Payment.PayeeAmountCents : 0,
            PaymentReference = r.Payment != null ? r.Payment.StripePaymentIntentId : null,
        };
}
