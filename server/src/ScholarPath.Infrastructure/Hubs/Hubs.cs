using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Hubs;

/// <summary>Base hub with JWT auth enforcement.</summary>
[Authorize]
public abstract class AuthenticatedHub : Hub
{
    public Task<string> Ping() => Task.FromResult($"pong from {GetType().Name} at {DateTimeOffset.UtcNow:O}");
}

/// <summary>Real-time 1:1 chat (PB-007).</summary>
public sealed class ChatHub(IPresenceTracker presence, IApplicationDbContext db) : AuthenticatedHub
{
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
        var userId = Context.UserIdentifier;
        if (userId is not null && presence.Connect(userId))
        {
            // First live connection for this user — announce them online to
            // everyone else. The caller seeds its own list via GetOnlineUsers,
            // so it does not need (and should not get) this echo.
            await Clients.Others.SendAsync("UserOnline", userId);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (userId is not null && presence.Disconnect(userId))
        {
            // Last live connection dropped (ref-count hit zero) — now offline.
            await Clients.Others.SendAsync("UserOffline", userId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Returns every currently-online user id so a freshly-connected client can
    /// seed its presence state instead of waiting for change events to trickle in.
    /// </summary>
    public IReadOnlyList<string> GetOnlineUsers() => presence.OnlineUsers();

    public async Task JoinConversation(string conversationId)
    {
        await EnsureParticipantAsync(conversationId);
        await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");
    }

    public Task LeaveConversation(string conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public async Task TypingStart(string conversationId)
    {
        await EnsureParticipantAsync(conversationId);
        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStart", conversationId, Context.UserIdentifier);
    }

    public async Task TypingStop(string conversationId)
    {
        await EnsureParticipantAsync(conversationId);
        await Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStop", conversationId, Context.UserIdentifier);
    }

    /// <summary>
    /// SEC-02 IDOR gate: the JWT-derived caller (<see cref="HubCallerContext.UserIdentifier"/>)
    /// must be one of the two participants of the conversation before it can join
    /// the group or emit typing events — otherwise any authenticated user could
    /// subscribe to <c>conversation:{id}</c> and eavesdrop on private message
    /// bodies fanned out by ChatRealtimeNotifier. Throws <see cref="HubException"/>
    /// on a malformed id or a non-participant.
    /// </summary>
    private async Task EnsureParticipantAsync(string conversationId)
    {
        if (!Guid.TryParse(Context.UserIdentifier, out var userId))
            throw new HubException("Unauthorized.");
        if (!Guid.TryParse(conversationId, out var convId))
            throw new HubException("Invalid conversation id.");

        var isParticipant = await db.Conversations
            .AsNoTracking()
            .AnyAsync(c => c.Id == convId
                && (c.ParticipantOneId == userId || c.ParticipantTwoId == userId));

        if (!isParticipant)
            throw new HubException("You are not a participant in this conversation.");
    }
}

/// <summary>Personal notifications stream (PB-010).</summary>
public sealed class NotificationHub : AuthenticatedHub
{
    public override async Task OnConnectedAsync()
    {
        // The user-group add MUST be awaited — a fire-and-forget call could let
        // a notification dispatched immediately after the connect race past the
        // group join and miss the recipient (so the dispatcher would resend on
        // the next reconnect, surfacing as a "duplicate" notification later).
        if (Context.UserIdentifier is not null)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{Context.UserIdentifier}").ConfigureAwait(false);
        }
        await base.OnConnectedAsync().ConfigureAwait(false);
    }
}

/// <summary>Community live updates (PB-007).</summary>
public sealed class CommunityHub(IApplicationDbContext db) : AuthenticatedHub
{
    public async Task JoinCategory(string categorySlug)
    {
        // SEC-02: only real, active categories can be subscribed to.
        if (string.IsNullOrWhiteSpace(categorySlug))
            throw new HubException("Invalid category.");

        var exists = await db.ForumCategories
            .AsNoTracking()
            .AnyAsync(c => c.Slug == categorySlug && c.IsActive);
        if (!exists)
            throw new HubException("Category not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");
    }

    public Task LeaveCategory(string categorySlug) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");

    /// <summary>
    /// Joins the per-thread group so clients viewing a single thread receive
    /// the ReplyCreated event the realtime notifier fans out for that post.
    /// SEC-02: the post must exist and be publicly visible (not soft-deleted,
    /// moderation-removed, or auto-hidden) before it can be subscribed to.
    /// </summary>
    public async Task JoinPost(string postId)
    {
        if (!Guid.TryParse(postId, out var id))
            throw new HubException("Invalid post id.");

        var visible = await db.ForumPosts
            .AsNoTracking()
            .AnyAsync(p => p.Id == id
                && !p.IsDeleted
                && !p.IsAutoHidden
                && p.ModerationStatus == PostModerationStatus.Visible);
        if (!visible)
            throw new HubException("Post not found.");

        await Groups.AddToGroupAsync(Context.ConnectionId, $"forum-post:{postId}");
    }

    public Task LeavePost(string postId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-post:{postId}");
}
