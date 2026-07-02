using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Chat.EventHandlers;

/// <summary>
/// Fan-out for a new chat message — drives the bell-icon notification (and an
/// email if the recipient isn't actively watching the chat) so a message that
/// lands while the recipient is on another page never goes unnoticed (PB-007).
///
/// <para>
/// The realtime SignalR <c>MessageReceived</c> push on <see cref="IChatRealtimeNotifier"/>
/// is independent — that one only reaches subscribers currently in the
/// conversation group. This handler covers everyone else.
/// </para>
/// </summary>
public sealed class ChatMessageNotificationEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    IChatPresenceQuery chatPresence,
    ILogger<ChatMessageNotificationEventHandler> logger)
    : INotificationHandler<ChatMessageReceivedEvent>
{
    /// <summary>Max preview length so notification titles/bodies stay scannable.</summary>
    private const int PreviewMaxLength = 120;

    public async Task Handle(ChatMessageReceivedEvent notification, CancellationToken ct)
    {
        // Skip the entire fan-out when the recipient is actively watching chat —
        // they already received the message via the live MessageReceived push,
        // and a bell ping / email on every keystroke would be noise.
        if (chatPresence.IsOnline(notification.RecipientId))
        {
            return;
        }

        var senderName = await db.Users
            .Where(u => u.Id == notification.SenderId)
            .Select(u => (u.FirstName + " " + u.LastName).Trim())
            .FirstOrDefaultAsync(ct);

        // Recipient's active role decides which messages page to deep-link to
        // — student-only routes 404 for consultants and companies. The
        // recipient picks the conversation from the sidebar once they land.
        var recipientRole = await db.Users
            .Where(u => u.Id == notification.RecipientId)
            .Select(u => u.ActiveRole)
            .FirstOrDefaultAsync(ct);
        var deepLink = recipientRole switch
        {
            "Consultant" => "/consultant/messages",
            "ScholarshipProvider"    => "/company/messages",
            _            => "/student/messages",
        };

        var preview = TrimPreview(notification.BodyPreview);

        try
        {
            await notifications.DispatchAsync(
                notification.RecipientId,
                NotificationType.ChatMessageReceived,
                new NotificationParams
                {
                    CounterpartyName = string.IsNullOrWhiteSpace(senderName) ? null : senderName,
                    Preview = preview,
                },
                deepLink: deepLink,
                idempotencyKey: $"chat-message:{notification.MessageId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A notification-channel failure must never break the send pipeline.
            logger.LogWarning(ex,
                "ChatMessageReceived notification failed for message {MessageId}.",
                notification.MessageId);
        }
    }

    private static string? TrimPreview(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;

        var trimmed = body.Trim();
        if (trimmed.Length <= PreviewMaxLength) return trimmed;

        return trimmed[..PreviewMaxLength].TrimEnd() + "…";
    }
}
