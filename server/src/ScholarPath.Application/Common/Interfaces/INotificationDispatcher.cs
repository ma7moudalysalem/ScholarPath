using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        Guid recipientUserId,
        NotificationType type,
        NotificationContent content,
        string? deepLink,
        string? idempotencyKey,
        CancellationToken ct);

    Task DispatchBroadcastAsync(
        IReadOnlyCollection<Guid> recipientUserIds,
        NotificationType type,
        NotificationContent content,
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
