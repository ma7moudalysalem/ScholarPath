using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.CaptureCompanyReviewPayment;

public sealed record CaptureCompanyReviewPaymentCommand(
    Guid ApplicationId) : IRequest<bool>;

public sealed class CaptureCompanyReviewPaymentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<CaptureCompanyReviewPaymentCommandHandler> logger)
    : IRequestHandler<CaptureCompanyReviewPaymentCommand, bool>
{
    public async Task<bool> Handle(CaptureCompanyReviewPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.ApplicationTrackerId == request.ApplicationId && p.Status == PaymentStatus.Held, ct);

        if (payment == null) return false;

        try
        {
            var stripeResult = await stripeService.CapturePaymentIntentAsync(
                payment.StripePaymentIntentId,
                null,
                Guid.NewGuid().ToString("N"),
                ct);

            if (stripeResult.Status == "succeeded")
            {
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                await notifications.DispatchAsync(
                    payment.CompanyId,
                    NotificationType.CompanyReviewPaymentSuccess,
                    new NotificationContent("Payment Captured", $"Payment for reviewing application {request.ApplicationId} has been captured.", null),
                    null,
                    null,
                    ct);

                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to capture payment for application {ApplicationId}", request.ApplicationId);
            return false;
        }
    }
}
