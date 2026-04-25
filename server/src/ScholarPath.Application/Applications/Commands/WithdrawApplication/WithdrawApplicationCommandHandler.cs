using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
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

        application.Status = ApplicationStatus.Withdrawn;
        application.WithdrawnAt = DateTimeOffset.UtcNow;
        application.IsReadOnly = true;

        var payment = await db.CompanyReviewPayments
            .FirstOrDefaultAsync(p => p.ApplicationTrackerId == application.Id && p.Status == PaymentStatus.Held, ct);

        if (payment != null)
        {
            // Refund policy: withdraws after submit -> refund 50%. Since it's held, it's considered "after submit".
            var refundCommand = new ScholarPath.Application.CompanyReviews.Commands.RefundCompanyReview.RefundCompanyReviewCommand(application.Id, IsFullRefund: false);
            await sender.Send(refundCommand, ct);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await notifications.DispatchAsync(
            application.StudentId,
            NotificationType.ApplicationWithdrawn,
            new NotificationContent("Application Withdrawn", "تم سحب الطلب", "Your application has been successfully withdrawn.", "تم سحب طلبك بنجاح.", null),             null,
            null,
            ct);

        logger.LogInformation("Student {StudentId} withdrew application {ApplicationId}",
            currentUser.UserId, application.Id);

        return true;
    }
}
