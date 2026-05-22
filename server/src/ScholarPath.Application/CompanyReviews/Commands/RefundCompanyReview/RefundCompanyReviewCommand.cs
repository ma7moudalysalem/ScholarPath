using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Payments;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview;

[Auditable(AuditAction.PaymentRefunded, "Payment",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Refunded CompanyReview payment for application {ApplicationId} — full={IsFullRefund}")]
public sealed record RefundCompanyReviewCommand(
    Guid ApplicationId,
    bool IsFullRefund) : IRequest<bool>;

/// <summary>
/// Refunds (full or 50%) the CompanyReview payment for an application. Works
/// off the unified <see cref="Domain.Entities.Payment"/> row, choosing the
/// correct Stripe action based on the payment's current state:
/// <list type="bullet">
///   <item>Held — cancel the PaymentIntent (no charge ever lands).</item>
///   <item>Captured — issue a full or 50% Stripe Refund.</item>
/// </list>
/// </summary>
public sealed class RefundCompanyReviewCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<RefundCompanyReviewCommandHandler> logger)
    : IRequestHandler<RefundCompanyReviewCommand, bool>
{
    public async Task<bool> Handle(RefundCompanyReviewCommand request, CancellationToken ct)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Type == PaymentType.CompanyReview
                && p.RelatedApplicationId == request.ApplicationId
                && (p.Status == PaymentStatus.Held
                    || p.Status == PaymentStatus.Pending
                    || p.Status == PaymentStatus.Captured
                    || p.Status == PaymentStatus.PartiallyRefunded),
                ct);

        if (payment is null)
        {
            logger.LogInformation(
                "No refundable CompanyReview payment found for application {ApplicationId}.",
                request.ApplicationId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            logger.LogWarning(
                "CompanyReview payment {PaymentId} has no Stripe PaymentIntent id; cannot refund.",
                payment.Id);
            return false;
        }

        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        var nowUtc = DateTimeOffset.UtcNow;

        try
        {
            // PATH A — Held / Pending: cancel the intent (no charge).
            if (payment.Status is PaymentStatus.Held or PaymentStatus.Pending)
            {
                if (!request.IsFullRefund)
                {
                    throw new ConflictException(
                        "Cannot issue a partial refund against a payment that has not been captured yet.");
                }

                await stripeService.CancelHeldPaymentAsync(
                    payment.StripePaymentIntentId,
                    idempotencyKey: $"company-review-refund:{payment.Id:N}:cancel",
                    ct: ct);

                payment.Status = PaymentStatus.Cancelled;
                payment.RefundedAmountCents = 0;
                payment.ProfitShareAmountCents = 0;
                payment.PayeeAmountCents = 0;
                payment.RefundedAt = null;
                payment.RefundReason = request.IsFullRefund
                    ? "Full refund — intent cancelled before capture"
                    : "Cancelled";
            }
            else
            {
                // PATH B — Captured / PartiallyRefunded: Stripe Refund.
                var refundAmountCents = request.IsFullRefund
                    ? payment.AmountCents - payment.RefundedAmountCents
                    : payment.AmountCents / 2;

                if (payment.RefundedAmountCents + refundAmountCents > payment.AmountCents)
                {
                    throw new ConflictException(
                        "Refund would exceed the captured amount of this payment.");
                }

                var idempotencyKey = request.IsFullRefund
                    ? $"company-review-refund:{payment.Id:N}:full"
                    : $"company-review-refund:{payment.Id:N}:partial";

                var refundResult = await stripeService.RefundPaymentAsync(
                    paymentIntentId: payment.StripePaymentIntentId,
                    amountCents: refundAmountCents,
                    reason: "requested_by_customer",
                    idempotencyKey: idempotencyKey,
                    ct: ct);

                if (refundResult.Status != "succeeded")
                {
                    logger.LogError(
                        "Refund for application {ApplicationId} did not succeed (Stripe status {Status}). Payment unchanged.",
                        request.ApplicationId, refundResult.Status);
                    return false;
                }

                payment.RefundedAmountCents += refundResult.AmountCents;
                payment.RefundedAt = nowUtc;
                payment.RefundReason = request.IsFullRefund
                    ? "Full refund — Stripe Refund issued"
                    : "Partial (50%) refund — Stripe Refund issued";

                payment.Status = payment.RefundedAmountCents >= payment.AmountCents
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                // PB-014 v1: commission tracks retained amount, not gross.
                var retainedSplit = await FinancialRuleResolver.ResolveSplitFromRetainedAsync(
                    db, payment.Type, payment.AmountCents, payment.RefundedAmountCents, ct);
                payment.ProfitShareAmountCents = retainedSplit.PlatformTakeCents;
                payment.PayeeAmountCents = retainedSplit.PayeeNetCents;
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            if (application is not null)
            {
                await notifications.DispatchAsync(
                    application.StudentId,
                    NotificationType.CompanyReviewRefunded,
                    new NotificationParams { RefundKind = request.IsFullRefund ? "Full" : "Partial" },
                    deepLink: null,
                    idempotencyKey: $"company-review-refund-notice:{payment.Id:N}:{(request.IsFullRefund ? "full" : "partial")}",
                    ct).ConfigureAwait(false);
            }

            return true;
        }
        catch (ConflictException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to refund CompanyReview payment for application {ApplicationId}",
                request.ApplicationId);
            return false;
        }
    }
}
