using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Events;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.Application.Community.EventHandlers;

public sealed class ForumPostCreatedEventHandler(
    IHubContext<CommunityHub> hubContext,
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
            await hubContext.Clients.Group($"forum-category:{category.Slug}")
                .SendAsync("NewPostCreated", notification.PostId, ct);
        }
    }
}

public sealed class ForumReplyCreatedEventHandler(
    IHubContext<CommunityHub> hubContext)
    : INotificationHandler<ForumReplyCreatedEvent>
{
    public async Task Handle(ForumReplyCreatedEvent notification, CancellationToken ct)
    {
        // Broadcast new reply via CommunityHub (FR-108)
        // For replies, we might broadcast to the thread group if there is one.
        // Or we can just broadcast globally or to a specific group. For now, assuming threads have their own group.
        await hubContext.Clients.Group($"forum-thread:{notification.ParentPostId}")
            .SendAsync("NewReplyCreated", notification.ReplyId, ct);
    }
}
