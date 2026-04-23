using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
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
                await stripeService.CancelPaymentIntentAsync(
                    payment.StripePaymentIntentId, 
                    "requested_by_customer", 
                    Guid.NewGuid().ToString("N"), 
                    ct);

                payment.Status = PaymentStatus.Refunded;
                payment.RefundedAmountUsd = payment.AmountUsd;
                payment.RefundReason = "System triggered full refund";

                if (application != null)
                {
                    await notifications.DispatchAsync(
                        application.StudentId,
                        NotificationType.CompanyReviewRefunded,
                        new NotificationContent("Review Fee Refunded", $"Your application {application.Id} review fee was fully refunded.", null),
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
                    Guid.NewGuid().ToString("N"),
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
                            new NotificationContent("Partial Review Fee Refund", $"Your application {application.Id} review fee was 50% refunded.", null),
                            null,
                            null,
                            ct);
                    }
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
