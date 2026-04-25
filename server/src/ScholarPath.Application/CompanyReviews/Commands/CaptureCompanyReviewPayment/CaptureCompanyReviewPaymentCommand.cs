using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;


namespace ScholarPath.Application.CompanyReviews.Commands.CaptureCompanyReviewPayment;
[Auditable(AuditAction.Update, "CompanyReviewPayment",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Captured payment for application {ApplicationId}")]
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
                $"company-review-capture:{payment.Id:N}",
                ct);

            if (stripeResult.Status == "succeeded")
            {
                payment.Status = PaymentStatus.Captured;
                payment.CapturedAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                await notifications.DispatchAsync(
                    payment.CompanyId,
                    NotificationType.CompanyReviewPaymentSuccess,
new NotificationContent("Payment Captured", "تم تحصيل الدفعة", $"Payment for reviewing application {request.ApplicationId} has been captured.", $"تم تحصيل دفعة مراجعة الطلب {request.ApplicationId}.", null),                    null,
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
