using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Dispatches a notification: renders text via the catalog from the type + params,
/// persists a Notification row per enabled channel, and delivers it (Task 5B).
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid recipientUserId,
        NotificationType type,
        NotificationParams parameters,
        string? deepLink,
        string? idempotencyKey,
        CancellationToken ct);

    Task DispatchBroadcastAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        NotificationType type,
        NotificationParams parameters,
        CancellationToken ct);
}

public sealed record NotificationContent(
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string? MetadataJson = null);

public interface IAuditService
{
    Task WriteAsync(
        AuditAction action,
        string targetType,
        Guid? targetId,
        string? beforeJson,
        string? afterJson,
        string? summary,
        CancellationToken ct);
}
