using MediatR;
using ScholarPath.Application.Notifications.DTOs;

namespace ScholarPath.Application.Notifications.Queries.GetNotificationPreferences;

/// <summary>
/// Returns the current user's notification-delivery preferences (FR-228) — one row
/// per <c>NotificationType</c> × <c>NotificationChannel</c>. Pairs the user has not
/// explicitly configured are reported as enabled (the platform default).
/// </summary>
public sealed record GetNotificationPreferencesQuery : IRequest<NotificationPreferencesDto>;
