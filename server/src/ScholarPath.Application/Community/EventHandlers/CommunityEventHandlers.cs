using MediatR;

using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
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
            // Broadcast new post via CommunityHub (FR-108)
            await communityNotifier.NotifyNewPostAsync(notification.PostId, category.Slug, ct);


        }
    }
}

public sealed class ForumReplyCreatedEventHandler(
    ICommunityRealtimeNotifier communityNotifier)
    : INotificationHandler<ForumReplyCreatedEvent>
{
    public async Task Handle(ForumReplyCreatedEvent notification, CancellationToken ct)
    {
        // Broadcast new reply via CommunityHub (FR-108)
        // For replies, we might broadcast to the thread group if there is one.
        // Or we can just broadcast globally or to a specific group. For now, assuming threads have their own group.
        await communityNotifier.NotifyNewReplyAsync(notification.ReplyId, notification.ParentPostId, ct);
    }
}
