using Microsoft.AspNetCore.SignalR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.Infrastructure.Services;

/// <summary>SignalR-backed implementation of <see cref="IChatRealtimeNotifier"/> (PB-007).</summary>
public sealed class ChatRealtimeNotifier(IHubContext<ChatHub> hub) : IChatRealtimeNotifier
{
    public Task NotifyNewMessageAsync(
        Guid conversationId,
        Guid messageId,
        Guid senderId,
        string body,
        DateTimeOffset sentAt,
        CancellationToken ct) =>
        hub.Clients
            .Group($"conversation:{conversationId}")
            .SendAsync(
                "MessageReceived",
                new { conversationId, messageId, senderId, body, sentAt },
                ct);
}
