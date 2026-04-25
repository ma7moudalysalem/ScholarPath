using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.ReviewApplication;

public sealed class ReviewApplicationCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    ISender sender,
    INotificationDispatcher notifications,
    ILogger<ReviewApplicationCommandHandler> logger)
    : IRequestHandler<ReviewApplicationCommand, bool>
{
    public async Task<bool> Handle(ReviewApplicationCommand request, CancellationToken ct)
    {
        var application = await db.Applications
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(Domain.Entities.ApplicationTracker), request.ApplicationId);

        if (application.Scholarship == null || application.Scholarship.OwnerCompanyId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        if (application.IsReadOnly)
        {
            throw new ConflictException("Application is already in a final state.");
        }

        var oldStatus = application.Status;
        application.Status = request.Status;
        application.DecisionAt = DateTimeOffset.UtcNow;
        application.DecisionReason = request.DecisionReason;
        application.IsReadOnly = true;

        application.RaiseDomainEvent(new ScholarPath.Domain.Events.ApplicationStatusChangedEvent(
            application.Id,
            application.StudentId,
            application.ScholarshipId,
            oldStatus,
            request.Status
        ));

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await notifications.DispatchAsync(
        application.StudentId,
        NotificationType.ApplicationStatusChanged,
        new NotificationContent("Application Update", "تحديث الطلب", $"Your application status is now {request.Status}.", $"حالة طلبك الآن {request.Status}.", null),
        null,
        null,
        ct);



        logger.LogInformation("Company {CompanyId} reviewed application {ApplicationId} as {Status}",
            currentUser.UserId, application.Id, request.Status);

        return true;
    }
}
