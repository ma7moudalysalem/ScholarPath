using Microsoft.AspNetCore.SignalR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.Infrastructure.Services;

/// <summary>SignalR-backed implementation of <see cref="ICommunityRealtimeNotifier"/> (PB-007).</summary>
public sealed class CommunityRealtimeNotifier(IHubContext<CommunityHub> hub) : ICommunityRealtimeNotifier
{
    public Task NotifyNewPostAsync(Guid postId, string categorySlug, CancellationToken ct) =>
        hub.Clients
            .Group($"forum-category:{categorySlug}")
            .SendAsync("PostCreated", new { postId, categorySlug }, ct);

    public Task NotifyNewReplyAsync(Guid replyId, Guid parentPostId, CancellationToken ct) =>
        hub.Clients
            .Group($"forum-post:{parentPostId}")
            .SendAsync("ReplyCreated", new { replyId, parentPostId }, ct);
}
