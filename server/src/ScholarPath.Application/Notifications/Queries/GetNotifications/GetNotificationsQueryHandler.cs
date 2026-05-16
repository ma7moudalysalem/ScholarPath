using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Queries.GetNotifications;

public sealed class GetNotificationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, NotificationsPageDto>
{
    public async Task<NotificationsPageDto> Handle(GetNotificationsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var baseQuery = db.Notifications.Where(n => n.RecipientUserId == userId);

        var total = await baseQuery.CountAsync(ct);
        var unreadCount = await baseQuery.CountAsync(n => !n.IsRead, ct);

        var items = await baseQuery
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(n => new NotificationDto(
                n.Id,
                n.Type.ToString(),
                n.TitleEn,
                n.TitleAr,
                n.BodyEn,
                n.BodyAr,
                n.DeepLink,
                n.IsRead,
                n.ReadAt,
                n.Priority,
                n.CreatedAt))
            .ToListAsync(ct);

        return new NotificationsPageDto(items, page, pageSize, total, unreadCount);
    }
}
