using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.CaptureCompanyReviewPayment;

[Auditable(AuditAction.PaymentCaptured, "Payment",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Captured CompanyReview payment for application {ApplicationId}")]
public sealed record CaptureCompanyReviewPaymentCommand(
    Guid ApplicationId) : IRequest<bool>;

/// <summary>
/// Captures the held CompanyReview PaymentIntent on the unified
/// <see cref="Domain.Entities.Payment"/> row identified by
/// <c>Type == CompanyReview &amp;&amp; RelatedApplicationId == ApplicationId</c>.
/// Called when the company finalises the application review (Accepted).
/// </summary>
public sealed class CaptureCompanyReviewPaymentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<CaptureCompanyReviewPaymentCommandHandler> logger)
    : IRequestHandler<CaptureCompanyReviewPaymentCommand, bool>
{
    public async Task<bool> Handle(CaptureCompanyReviewPaymentCommand request, CancellationToken ct)
    {
        // Look up the held CompanyReview Payment for this application. We accept
        // Held *or* Pending because the Stripe `amount_capturable_updated`
        // webhook may not have landed yet — the capture call itself proves the
        // intent is in `requires_capture`.
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Type == PaymentType.CompanyReview
                && p.RelatedApplicationId == request.ApplicationId
                && (p.Status == PaymentStatus.Held || p.Status == PaymentStatus.Pending),
                ct);

        if (payment is null)
        {
            logger.LogInformation(
                "No held CompanyReview payment found for application {ApplicationId} — nothing to capture.",
                request.ApplicationId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            logger.LogWarning(
                "CompanyReview payment {PaymentId} has no Stripe PaymentIntent id; cannot capture.",
                payment.Id);
            return false;
        }

        try
        {
            var stripeResult = await stripeService.CapturePaymentIntentAsync(
                payment.StripePaymentIntentId,
                amountToCaptureCents: null,
                idempotencyKey: $"company-review-capture:{payment.Id:N}",
                ct: ct);

            if (stripeResult.Status != "succeeded")
            {
                logger.LogWarning(
                    "Capture for application {ApplicationId} returned Stripe status {Status}; payment left as {Original}.",
                    request.ApplicationId, stripeResult.Status, payment.Status);
                return false;
            }

            var nowUtc = DateTimeOffset.UtcNow;
            payment.Status = PaymentStatus.Captured;
            payment.CapturedAt = nowUtc;
            if (!string.IsNullOrWhiteSpace(stripeResult.LatestChargeId))
            {
                payment.StripeChargeId = stripeResult.LatestChargeId;
            }

            // Re-resolve the split at capture time so the snapshot reflects the
            // rule in force right now (an admin can change the active rule
            // between intent creation and capture). PB-014 v1 = 10% default.
            var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
                db, payment.Type, payment.AmountCents, ct);
            payment.ProfitShareAmountCents = split.PlatformTakeCents;
            payment.PayeeAmountCents = split.PayeeNetCents;

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            if (payment.PayeeUserId is Guid payee)
            {
                await notifications.DispatchAsync(
                    payee,
                    NotificationType.CompanyReviewPaymentSuccess,
                    NotificationParams.Empty,
                    deepLink: null,
                    idempotencyKey: $"company-review-paid:{payment.Id:N}",
                    ct).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to capture CompanyReview payment for application {ApplicationId}",
                request.ApplicationId);
            return false;
        }
    }
}
