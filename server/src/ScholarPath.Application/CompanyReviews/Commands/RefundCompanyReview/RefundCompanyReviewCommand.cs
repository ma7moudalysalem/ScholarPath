using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Payments;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview;

public sealed record RefundCompanyReviewCommand(
    Guid ApplicationId,
    bool IsFullRefund) : IRequest<bool>;

public sealed class RefundCompanyReviewCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<RefundCompanyReviewCommandHandler> logger)
    : IRequestHandler<RefundCompanyReviewCommand, bool>
{
    public async Task<bool> Handle(RefundCompanyReviewCommand request, CancellationToken ct)
    {
        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.ApplicationTrackerId == request.ApplicationId && p.Status == PaymentStatus.Held, ct);

        if (payment == null) return false;

        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);

        try
        {
            if (request.IsFullRefund)
            {
                await stripeService.CancelHeldPaymentAsync(
                    payment.StripePaymentIntentId,
                    $"company-review-refund:{payment.Id:N}:full",
                    ct);

                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAmountUsd = payment.AmountUsd;
                payment.RefundReason = "System triggered full refund";

                if (application != null)
                {
                    await notifications.DispatchAsync(
                        application.StudentId,
                        NotificationType.CompanyReviewRefunded,
                        new NotificationParams { RefundKind = "Full" },
                        null,
                        null,
                        ct);
                }
            }
            else
            {
                long refundAmountCents = (long)(payment.AmountUsd * 50m);
                var captureAmountCents = (long)(payment.AmountUsd * 100m) - refundAmountCents;

                var stripeResult = await stripeService.CapturePaymentIntentAsync(
                    payment.StripePaymentIntentId,
                    captureAmountCents,
                    $"company-review-refund:{payment.Id:N}:partial",
                    ct);

                if (stripeResult.Status == "succeeded")
                {
                    payment.Status = PaymentStatus.PartiallyRefunded;
                    payment.CapturedAt = DateTimeOffset.UtcNow;
                    payment.RefundedAmountUsd = payment.AmountUsd - (captureAmountCents / 100m);
                    payment.RefundReason = "System triggered partial (50%) refund";

                    if (application != null)
                    {
                        await notifications.DispatchAsync(
                            application.StudentId,
                            NotificationType.CompanyReviewRefunded,
                            new NotificationParams { RefundKind = "Partial" },
                            null,
                            null,
                            ct);
                    }
                }
                else
                {
                    // Stripe did not confirm the partial capture — leave the payment
                    // as Held (don't persist a fake refund) and let the caller retry.
                    logger.LogError(
                        "Partial refund for application {ApplicationId} did not succeed (Stripe status {Status}). Payment left as Held.",
                        request.ApplicationId, stripeResult.Status);
                    return false;
                }
            }

            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refund company review payment for application {ApplicationId}", request.ApplicationId);
            return false;
        }
    }
}
