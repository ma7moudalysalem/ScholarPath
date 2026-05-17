using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Notifications;

/// <summary>
/// The single source of bilingual notification text (Task 5B). Renders the EN/AR
/// title and body for a notification type from structured params.
/// </summary>
public interface INotificationCatalog
{
    NotificationContent Render(NotificationType type, NotificationParams parameters);
}
