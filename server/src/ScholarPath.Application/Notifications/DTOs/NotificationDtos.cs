namespace ScholarPath.Application.Notifications.DTOs;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string TitleEn,
    string TitleAr,
    string BodyEn,
    string BodyAr,
    string? DeepLink,
    bool IsRead,
    DateTimeOffset? ReadAt,
    int Priority,
    DateTimeOffset CreatedAt);

public sealed record NotificationsPageDto(
    IReadOnlyList<NotificationDto> Items,
    int Page,
    int PageSize,
    int Total,
    int UnreadCount);
