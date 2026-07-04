using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Applications.EventHandlers;

/// <summary>
/// On submission, notifies BOTH the student (a submission confirmation) and the
/// scholarship's owning company (there's something to review) — FR-APP-17.
/// The student is always notified; the company only when the listing has an
/// owner (external / admin-owned listings have none).
/// </summary>
public sealed class ApplicationSubmittedNotificationEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications)
    : INotificationHandler<ApplicationSubmittedEvent>
{
    public async Task Handle(ApplicationSubmittedEvent notification, CancellationToken ct)
    {
        var scholarship = await db.Scholarships
            .AsNoTracking()
            .Where(s => s.Id == notification.ScholarshipId)
            .Select(s => new { s.OwnerScholarshipProviderId, s.TitleEn, s.TitleAr })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        // FR-APP-17: confirm submission to the student (they were previously only
        // given a timeline entry, no notification).
        await notifications.DispatchAsync(
            notification.StudentId,
            NotificationType.ApplicationSubmittedConfirmation,
            new NotificationParams { TitleEn = scholarship?.TitleEn, TitleAr = scholarship?.TitleAr },
            deepLink: $"/student/applications/{notification.ApplicationId}",
            idempotencyKey: $"app-submitted-student:{notification.ApplicationId}",
            ct).ConfigureAwait(false);

        if (scholarship?.OwnerScholarshipProviderId is not { } companyId)
            return; // No owning company (external/admin listing) — nothing more to notify.

        await notifications.DispatchAsync(
            companyId,
            NotificationType.ApplicationSubmitted,
            new NotificationParams { TitleEn = scholarship.TitleEn, TitleAr = scholarship.TitleAr },
            deepLink: "/company/applications-review",
            idempotencyKey: $"app-submitted:{notification.ApplicationId}",
            ct).ConfigureAwait(false);
    }
}
