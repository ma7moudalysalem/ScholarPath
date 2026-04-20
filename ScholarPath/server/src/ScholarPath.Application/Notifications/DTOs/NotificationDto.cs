using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Notifications.DTOs;

public record NotificationDto(
    Guid Id,
    NotificationType Type,
    string Title,
    string? TitleAr,
    string Message,
    string? MessageAr,
    bool IsRead,
    DateTime? ReadAt,
    Guid? RelatedEntityId,
    string? RelatedEntityType,
    DateTime CreatedAt);
    