using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Queries.GetUnreadCount;

public sealed class GetUnreadCountQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetUnreadCountQuery, int>
{
    public async Task<int> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Count only the in-app feed rows — the dispatcher writes one row per
        // channel (InApp + Email), so counting all channels doubled the badge.
        return await db.Notifications
            .CountAsync(
                n => n.RecipientUserId == userId
                     && n.Channel == NotificationChannel.InApp
                     && !n.IsRead,
                ct);
    }
}
