using MediatR;
using ScholarPath.Application.Notifications.DTOs;

namespace ScholarPath.Application.Notifications.Queries.GetNotifications;

public sealed record GetNotificationsQuery(int Page = 1, int PageSize = 20)
    : IRequest<NotificationsPageDto>;
