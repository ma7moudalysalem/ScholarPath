using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Payments;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface IScholarshipProviderReviewTimeoutRefundJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily job that refunds review fees for applications the company failed to
/// review within 14 days of the scholarship deadline (PB-005 AC#3 / FR-068).
/// Operates on the unified <see cref="Domain.Entities.Payment"/> table. The
/// refund action depends on the payment's current state:
/// <list type="bullet">
///   <item>Held — cancel the PaymentIntent (no charge).</item>
///   <item>Captured — issue a full Stripe Refund.</item>
/// </list>
/// </summary>
public sealed class ScholarshipProviderReviewTimeoutRefundJob(
    ApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<ScholarshipProviderReviewTimeoutRefundJob> logger) : IScholarshipProviderReviewTimeoutRefundJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Applications still awaiting a decision whose scholarship deadline
        // passed at least 14 days ago.
        var expiredApplications = await db.Applications
            .Include(a => a.Scholarship)
            .Where(a => (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.UnderReview)
                     && a.Scholarship != null
                     && a.Scholarship.Deadline.AddDays(14) < now)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (expiredApplications.Count == 0)
        {
            return;
        }

        int refunded = 0;

        foreach (var app in expiredApplications)
        {
            var payment = await db.Payments
                .FirstOrDefaultAsync(p =>
                    p.Type == PaymentType.ScholarshipProviderReview
                    && p.RelatedApplicationId == app.Id
                    && (p.Status == PaymentStatus.Held
                        || p.Status == PaymentStatus.Pending
                        || p.Status == PaymentStatus.Captured),
                    ct);

            if (payment is null) continue;
            if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId)) continue;

            try
            {
                if (payment.Status is PaymentStatus.Held or PaymentStatus.Pending)
                {
                    // Refund 100% by cancelling the hold — no charge ever lands.
                    await stripeService.CancelHeldPaymentAsync(
                        payment.StripePaymentIntentId,
                        idempotencyKey: $"company-review-timeout-refund:{payment.Id:N}:cancel",
                        ct: ct).ConfigureAwait(false);

                    payment.Status = PaymentStatus.Cancelled;
                    payment.RefundedAmountCents = 0;
                    payment.ProfitShareAmountCents = 0;
                    payment.PayeeAmountCents = 0;
                    payment.RefundReason = "ScholarshipProvider failed to review within 14 days after deadline";
                }
                else
                {
                    // Captured — issue a full Stripe Refund.
                    var refundAmountCents = payment.AmountCents - payment.RefundedAmountCents;
                    if (refundAmountCents <= 0) continue;

                    var refundResult = await stripeService.RefundPaymentAsync(
                        paymentIntentId: payment.StripePaymentIntentId,
                        amountCents: refundAmountCents,
                        reason: "requested_by_customer",
                        idempotencyKey: $"company-review-timeout-refund:{payment.Id:N}:full",
                        ct: ct).ConfigureAwait(false);

                    if (refundResult.Status != "succeeded")
                    {
                        logger.LogError(
                            "Timeout refund for application {ApplicationId} returned Stripe status {Status}; left unchanged.",
                            app.Id, refundResult.Status);
                        continue;
                    }

                    payment.RefundedAmountCents += refundResult.AmountCents;
                    payment.RefundedAt = now;
                    payment.RefundReason = "ScholarshipProvider failed to review within 14 days after deadline";
                    payment.Status = payment.RefundedAmountCents >= payment.AmountCents
                        ? PaymentStatus.Refunded
                        : PaymentStatus.PartiallyRefunded;

                    var retainedSplit = await FinancialRuleResolver.ResolveSplitFromRetainedAsync(
                        db, payment.Type, payment.AmountCents, payment.RefundedAmountCents, ct);
                    payment.ProfitShareAmountCents = retainedSplit.PlatformTakeCents;
                    payment.PayeeAmountCents = retainedSplit.PayeeNetCents;
                }

                await notifications.DispatchAsync(
                    app.StudentId,
                    NotificationType.ScholarshipProviderReviewRefunded,
                    new NotificationParams { RefundKind = "Timeout" },
                    deepLink: null,
                    idempotencyKey: $"company-review-timeout-refund-notice:{payment.Id:N}",
                    ct).ConfigureAwait(false);

                refunded++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to refund expired review for application {ApplicationId}",
                    app.Id);
            }
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "ScholarshipProviderReviewTimeoutRefundJob — scanned {Scanned} expired applications, refunded {Refunded}.",
            expiredApplications.Count, refunded);
    }
}
