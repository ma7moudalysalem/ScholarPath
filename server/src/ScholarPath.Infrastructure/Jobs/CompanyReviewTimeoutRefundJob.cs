using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface ICompanyReviewTimeoutRefundJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily job to auto-refund review fees for applications that companies failed
/// to review within 14 days of the deadline.
/// </summary>
public sealed class CompanyReviewTimeoutRefundJob(
    ApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<CompanyReviewTimeoutRefundJob> logger) : ICompanyReviewTimeoutRefundJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Find applications where Status is Pending or UnderReview,
        // the scholarship deadline has passed by 14 days,
        // and there is a held payment.
        var expiredApplications = await db.Applications
            .Include(a => a.Scholarship)
            .Where(a => (a.Status == ApplicationStatus.Pending || a.Status == ApplicationStatus.UnderReview)
                     && a.Scholarship != null
                     && a.Scholarship.Deadline.AddDays(14) < now)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var app in expiredApplications)
        {
            var payment = await db.CompanyReviewPayments
                .FirstOrDefaultAsync(p => p.ApplicationTrackerId == app.Id && p.Status == PaymentStatus.Held, ct);

            if (payment != null)
            {
                try
                {
                    // Refund 100%
                    var stripeResult = await stripeService.CancelPaymentIntentAsync(
                        payment.StripePaymentIntentId,
                        "requested_by_customer",
                        $"company-review-timeout-refund:{payment.Id:N}",
                        ct);

                    payment.Status = PaymentStatus.Refunded;
                    payment.RefundedAmountUsd = payment.AmountUsd;
                    payment.RefundReason = "Company failed to review within 14 days after deadline";

                    // Notify student
                   await notifications.DispatchAsync(
                    app.StudentId,
                    NotificationType.CompanyReviewRefunded,
                 new NotificationContent("Review Fee Refunded", "تم استرداد رسوم المراجعة", $"The company failed to review your application within 14 days. Your fee has been refunded.", "لم تقم الشركة بمراجعة طلبك خلال 14 يومًا. تم استرداد الرسوم.", null),
                 null,
                 null,
                  ct);

                    // Mark application as expired or something.
                    // Let's just set it to Withdrawn, or maybe we just leave it?
                    // We'll leave it pending/under review but refunded, or maybe we reject it automatically?
                    // Let's just leave the status but note that the payment is refunded.
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to refund expired review for application {ApplicationId}", app.Id);
                }
            }
        }

        if (expiredApplications.Any())
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Processed {Count} expired application reviews and refunded fees.", expiredApplications.Count);
        }
    }
}
