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

        // Batch-load the refundable payments for all expired applications in ONE query
        // (was an N+1 — one FirstOrDefault per application).
        var appIds = expiredApplications.Select(a => a.Id).ToList();
        var paymentsByApp = (await db.Payments
                .Where(p => p.Type == PaymentType.ScholarshipProviderReview
                            && p.RelatedApplicationId != null
                            && appIds.Contains(p.RelatedApplicationId.Value)
                            && (p.Status == PaymentStatus.Held
                                || p.Status == PaymentStatus.Pending
                                || p.Status == PaymentStatus.Captured))
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .GroupBy(p => p.RelatedApplicationId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        int refunded = 0;

        foreach (var app in expiredApplications)
        {
            if (!paymentsByApp.TryGetValue(app.Id, out var payment)) continue;
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

                // Persist THIS application's refund immediately — each refund is its own
                // unit of work. Previously a single SaveChanges after the whole loop meant
                // a mid-loop crash left Stripe having refunded N payments while zero DB rows
                // were saved (they'd be reprocessed next run — safe via the Stripe idempotency
                // key, but fragile). Saving per iteration bounds that window to one row.
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

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

        logger.LogInformation(
            "ScholarshipProviderReviewTimeoutRefundJob — scanned {Scanned} expired applications, refunded {Refunded}.",
            expiredApplications.Count, refunded);
    }
}
