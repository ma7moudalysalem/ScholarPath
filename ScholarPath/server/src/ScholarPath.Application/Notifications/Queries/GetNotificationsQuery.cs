using MediatR;
using ScholarPath.Application.Notifications.DTOs;

namespace ScholarPath.Application.Notifications.Queries.GetNotifications;

public record GetNotificationsQuery(
    int Page = 1,
    int PageSize = 20) : IRequest<PaginatedNotificationResponse>;

public record PaginatedNotificationResponse(
    List<NotificationDto> Items,
    int TotalCount,
    int Page,
    int PageSize);
    