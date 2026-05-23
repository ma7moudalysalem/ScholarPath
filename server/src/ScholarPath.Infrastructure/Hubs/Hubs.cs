using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ScholarPath.Infrastructure.Hubs;

/// <summary>Base hub with JWT auth enforcement.</summary>
[Authorize]
public abstract class AuthenticatedHub : Hub
{
    public Task<string> Ping() => Task.FromResult($"pong from {GetType().Name} at {DateTimeOffset.UtcNow:O}");
}

/// <summary>Real-time 1:1 chat (PB-007).</summary>
public sealed class ChatHub(IPresenceTracker presence) : AuthenticatedHub
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

    public Task JoinConversation(string conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public Task LeaveConversation(string conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public Task TypingStart(string conversationId) =>
        Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStart", conversationId, Context.UserIdentifier);

    public Task TypingStop(string conversationId) =>
        Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStop", conversationId, Context.UserIdentifier);
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
public sealed class CommunityHub : AuthenticatedHub
{
    public Task JoinCategory(string categorySlug) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");

    public Task LeaveCategory(string categorySlug) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");

    /// <summary>
    /// Joins the per-thread group so clients viewing a single thread receive
    /// the ReplyCreated event the realtime notifier fans out for that post.
    /// </summary>
    public Task JoinPost(string postId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"forum-post:{postId}");

    public Task LeavePost(string postId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-post:{postId}");
}
