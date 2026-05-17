using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Community.EventHandlers;

/// <summary>
/// Notifies platform admins when a post crosses the auto-hide flag threshold so it
/// gets a timely human moderation decision. FlagPost has already set the post's
/// ModerationStatus to PendingReview (the admin moderation queue).
/// </summary>
public sealed class PostAutoHiddenEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<PostAutoHiddenEventHandler> logger)
    : INotificationHandler<PostAutoHiddenEvent>
{
    public async Task Handle(PostAutoHiddenEvent notification, CancellationToken ct)
    {
        var adminIds = await db.Users
            .Where(u => u.ActiveRole == "Admin" && u.AccountStatus == AccountStatus.Active)
            .Select(u => u.Id)
            .ToListAsync(ct);

        if (adminIds.Count == 0)
        {
            logger.LogWarning(
                "Post {PostId} auto-hidden after {FlagCount} flags but no admin recipients were found.",
                notification.PostId, notification.FlagCount);
            return;
        }

        await notifications.DispatchBroadcastAsync(
            adminIds,
            NotificationType.PostAutoHidden,
            new NotificationParams { Count = notification.FlagCount },
            ct);
    }
}
