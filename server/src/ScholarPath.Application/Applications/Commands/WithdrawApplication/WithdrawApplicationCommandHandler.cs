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

        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.ApplicationTrackerId == application.Id && p.Status == PaymentStatus.Held, ct);

        if (payment != null)
        {
            // Refund policy: withdraw before the company starts reviewing -> 100%;
            // withdraw once it is already under review -> 50%.
            var isFullRefund = statusBeforeWithdrawal
                is ApplicationStatus.Draft or ApplicationStatus.Pending;
            var refundCommand = new ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview.RefundCompanyReviewCommand(
                application.Id, IsFullRefund: isFullRefund);
            await sender.Send(refundCommand, ct);
        }

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
