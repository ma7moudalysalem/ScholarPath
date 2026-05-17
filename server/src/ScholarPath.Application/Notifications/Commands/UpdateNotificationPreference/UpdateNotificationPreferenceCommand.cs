using MediatR;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Notifications.Commands.UpdateNotificationPreference;

/// <summary>
/// Enables or disables one notification <see cref="Channel"/> for one
/// <see cref="Type"/> for the current user (FR-228). Upserts the underlying
/// <c>NotificationPreference</c> row.
/// </summary>
public sealed record UpdateNotificationPreferenceCommand(
    NotificationType Type,
    NotificationChannel Channel,
    bool IsEnabled) : IRequest;
