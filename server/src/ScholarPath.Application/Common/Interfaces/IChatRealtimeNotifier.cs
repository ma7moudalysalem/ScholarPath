namespace ScholarPath.Application.Common.Interfaces;

public interface IChatRealtimeNotifier
{
    Task NotifyNewMessageAsync(Guid conversationId, Guid messageId, Guid senderId, string body, DateTimeOffset sentAt, CancellationToken ct);
}
