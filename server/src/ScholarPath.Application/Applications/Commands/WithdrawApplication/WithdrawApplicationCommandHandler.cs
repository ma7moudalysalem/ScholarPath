using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.WithdrawApplication;

public sealed class WithdrawApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISender sender,
    INotificationDispatcher notifications,
    ILogger<WithdrawApplicationCommandHandler> logger)
    : IRequestHandler<WithdrawApplicationCommand, bool>
{
    public async Task<bool> Handle(WithdrawApplicationCommand request, CancellationToken ct)
    {
        var application = await db.Applications
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.ApplicationId);

        if (application.StudentId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        if (application.IsReadOnly || application.Status == ApplicationStatus.Withdrawn)
        {
            throw new ConflictException("Application cannot be withdrawn at this stage.");
        }

        // Capture the pre-withdrawal status — it decides the refund tier below.
        var statusBeforeWithdrawal = application.Status;

        application.Status = ApplicationStatus.Withdrawn;
        application.WithdrawnAt = DateTimeOffset.UtcNow;

        // PB-005 v1: company-review payments live on the unified Payment table
        // (Type = ScholarshipProviderReview). A payment qualifies for the withdrawal refund
        // flow only when it has not yet been finalised — i.e. it is still Held
        // (pre-acceptance) or already Captured (post-acceptance, under review).
        var payment = await db.Payments
            .FirstOrDefaultAsync(p =>
                p.Type == PaymentType.ScholarshipProviderReview
                && p.RelatedApplicationId == application.Id
                && (p.Status == PaymentStatus.Held
                    || p.Status == PaymentStatus.Pending
                    || p.Status == PaymentStatus.Captured),
                ct);

        if (payment != null)
        {
            // Refund policy: withdraw before the company starts reviewing -> 100%
            // (hold cancelled, no charge), withdraw once it is already under review
            // -> 50% (Stripe Refund issued).
            var isFullRefund = statusBeforeWithdrawal
                is ApplicationStatus.Draft or ApplicationStatus.Pending;
            var refundCommand = new ScholarPath.Application.ScholarshipProviderReviews.Commands.RefundScholarshipProviderReview.RefundScholarshipProviderReviewCommand(
                application.Id, IsFullRefund: isFullRefund);
            await sender.Send(refundCommand, ct);
        }

        // FR-APP-18/19: record the withdrawal on the student's status timeline.
        // Submit and Review raise this event to append a StatusHistory row; withdraw
        // did not, so a withdrawal never appeared in the timeline. The payment-outcome
        // handler only acts on Accepted/Rejected, so raising it here is side-effect-safe.
        application.RaiseDomainEvent(new ScholarPath.Domain.Events.ApplicationStatusChangedEvent(
            application.Id,
            application.StudentId,
            application.ScholarshipId,
            statusBeforeWithdrawal,
            ApplicationStatus.Withdrawn
        ));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await notifications.DispatchAsync(
            application.StudentId,
            NotificationType.ApplicationWithdrawn,
            NotificationParams.Empty,
            null,
            null,
            ct);

        logger.LogInformation("Student {StudentId} withdrew application {ApplicationId}",
            currentUser.UserId, application.Id);

        return true;
    }
}
