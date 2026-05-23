using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Payments;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.CompanyReviews.Commands.RejectCompanyReviewPayment;

[Auditable(AuditAction.Update, "Payment",
    TargetIdProperty = nameof(ApplicationId),
    SummaryTemplate = "Cancelled CompanyReview payment hold for rejected application {ApplicationId}")]
public sealed record RejectCompanyReviewPaymentCommand(
    Guid ApplicationId) : IRequest<bool>;

/// <summary>
/// Cancels the held CompanyReview PaymentIntent when a company rejects an
/// application before any capture has occurred. No money changes hands. Used
/// by <c>ApplicationStatusChangedEventHandler</c> on the Rejected transition.
/// </summary>
public sealed class RejectCompanyReviewPaymentCommandHandler(
    IApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<RejectCompanyReviewPaymentCommandHandler> logger)
    : IRequestHandler<RejectCompanyReviewPaymentCommand, bool>
{
    public async Task<bool> Handle(RejectCompanyReviewPaymentCommand request, CancellationToken ct)
    {
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Type == PaymentType.CompanyReview
                && p.RelatedApplicationId == request.ApplicationId
                && (p.Status == PaymentStatus.Held || p.Status == PaymentStatus.Pending),
                ct);

        // If no held payment exists, the rejection has nothing to cancel —
        // not all applications carry a review fee.
        if (payment is null)
        {
            logger.LogInformation(
                "No held CompanyReview payment to cancel for rejected application {ApplicationId}.",
                request.ApplicationId);
            return false;
        }

        if (string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            logger.LogWarning(
                "CompanyReview payment {PaymentId} has no Stripe PaymentIntent id; cannot cancel.",
                payment.Id);
            return false;
        }

        try
        {
            await stripeService.CancelHeldPaymentAsync(
                payment.StripePaymentIntentId,
                idempotencyKey: $"company-review-reject:{payment.Id:N}",
                ct: ct).ConfigureAwait(false);

            payment.Status = PaymentStatus.Cancelled;
            payment.RefundedAmountCents = 0;
            payment.ProfitShareAmountCents = 0;
            payment.PayeeAmountCents = 0;
            payment.FailureReason = "company_rejected_application";

            await db.SaveChangesAsync(ct).ConfigureAwait(false);

            var application = await db.Applications
                .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct);
            if (application is not null)
            {
                await notifications.DispatchAsync(
                    application.StudentId,
                    NotificationType.CompanyReviewRefunded,
                    new NotificationParams { RefundKind = "Rejection" },
                    deepLink: null,
                    idempotencyKey: $"company-review-reject-notice:{payment.Id:N}",
                    ct).ConfigureAwait(false);
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to cancel CompanyReview payment hold for rejected application {ApplicationId}",
                request.ApplicationId);
            return false;
        }
    }
}
