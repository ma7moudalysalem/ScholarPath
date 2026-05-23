using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Community.EventHandlers;

public sealed class ForumPostCreatedEventHandler(
    ICommunityRealtimeNotifier communityNotifier,
    IApplicationDbContext db)
    : INotificationHandler<ForumPostCreatedEvent>
{
    public async Task Handle(ForumPostCreatedEvent notification, CancellationToken ct)
    {
        var category = await db.ForumCategories
            .FirstOrDefaultAsync(c => c.Id == notification.CategoryId, ct);

        if (category != null)
        {
            await communityNotifier.NotifyNewPostAsync(notification.PostId, category.Slug, ct);
        }
    }
}

/// <summary>
/// Handles a new reply: broadcasts it via SignalR for live thread updates,
/// AND notifies the parent post's author so they see "Someone replied to your
/// post" in their bell drawer. Skips the notification when the replier is
/// replying to themselves.
/// </summary>
public sealed class ForumReplyCreatedEventHandler(
    ICommunityRealtimeNotifier communityNotifier,
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<ForumReplyCreatedEventHandler> logger)
    : INotificationHandler<ForumReplyCreatedEvent>
{
    public async Task Handle(ForumReplyCreatedEvent notification, CancellationToken ct)
    {
        // 1) Real-time fan-out to anyone currently watching this thread.
        await communityNotifier.NotifyNewReplyAsync(notification.ReplyId, notification.ParentPostId, ct);

        // 2) Persistent "reply on your post" notification — best-effort, never
        //    blocks the reply itself if dispatch fails.
        try
        {
            var parent = await db.ForumPosts
                .AsNoTracking()
                .Where(p => p.Id == notification.ParentPostId)
                .Select(p => new { p.AuthorId, p.Title, RootPostId = p.ParentPostId ?? p.Id })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            if (parent is null) return;

            // Don't notify yourself for your own reply on your own post/reply.
            if (parent.AuthorId == notification.AuthorId) return;

            var replier = await db.Users
                .AsNoTracking()
                .Where(u => u.Id == notification.AuthorId)
                .Select(u => new { u.FirstName, u.LastName })
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var replierName = replier is null
                ? null
                : $"{replier.FirstName} {replier.LastName}".Trim();

            await notifications.DispatchAsync(
                parent.AuthorId,
                NotificationType.ReplyOnYourPost,
                new NotificationParams
                {
                    TitleEn = parent.Title,
                    TitleAr = parent.Title,
                    CounterpartyName = replierName,
                },
                deepLink: $"/student/community/{parent.RootPostId}",
                idempotencyKey: $"reply-on-post:{notification.ParentPostId:N}:{notification.ReplyId:N}:{parent.AuthorId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to dispatch ReplyOnYourPost notification for reply {ReplyId}.", notification.ReplyId);
        }
    }
}
