using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Applications.EventHandlers;

/// <summary>
/// Notifies the scholarship's owning company when a student submits a new in-app
/// application, so the company knows there's something to review (QA BUG-017 —
/// previously SubmitApplication notified no one but the student's own timeline).
/// External / admin-owned listings (no owning company) are skipped.
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
            .Select(s => new { s.OwnerCompanyId, s.TitleEn, s.TitleAr })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (scholarship?.OwnerCompanyId is not { } companyId)
            return; // No owning company (external/admin listing) — nothing to notify.

        await notifications.DispatchAsync(
            companyId,
            NotificationType.ApplicationSubmitted,
            new NotificationParams { TitleEn = scholarship.TitleEn, TitleAr = scholarship.TitleAr },
            deepLink: "/company/applications-review",
            idempotencyKey: $"app-submitted:{notification.ApplicationId}",
            ct).ConfigureAwait(false);
    }
}
