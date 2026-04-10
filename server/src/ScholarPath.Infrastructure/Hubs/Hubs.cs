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
public sealed class ChatHub : AuthenticatedHub
{
    public Task JoinConversation(string conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public Task LeaveConversation(string conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation:{conversationId}");

    public Task TypingStart(string conversationId) =>
        Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStart", conversationId);

    public Task TypingStop(string conversationId) =>
        Clients.OthersInGroup($"conversation:{conversationId}").SendAsync("TypingStop", conversationId);
}

/// <summary>Personal notifications stream (PB-010).</summary>
public sealed class NotificationHub : AuthenticatedHub
{
    public override Task OnConnectedAsync()
    {
        if (Context.UserIdentifier is not null)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, $"user:{Context.UserIdentifier}");
        }
        return base.OnConnectedAsync();
    }
}

/// <summary>Community live updates (PB-007).</summary>
public sealed class CommunityHub : AuthenticatedHub
{
    public Task JoinCategory(string categorySlug) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");

    public Task LeaveCategory(string categorySlug) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"forum-category:{categorySlug}");
}
