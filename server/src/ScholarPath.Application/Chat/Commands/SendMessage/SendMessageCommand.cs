using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Chat.Commands.SendMessage;
[Auditable(AuditAction.Create, "ChatMessage",
    TargetIdProperty = nameof(RecipientId),
    SummaryTemplate = "Sent message to {RecipientId}")]
public sealed record SendMessageCommand(
    Guid RecipientId,
    string Body) : IRequest<Guid>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(v => v.RecipientId).NotEmpty();
        RuleFor(v => v.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class SendMessageCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IChatRealtimeNotifier chatNotifier,
    IPublisher publisher)
    : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var senderId = currentUser.UserId ?? throw new ForbiddenAccessException();

        var conversation = await db.Conversations
            .FirstOrDefaultAsync(c =>
                (c.ParticipantOneId == senderId && c.ParticipantTwoId == request.RecipientId) ||
                (c.ParticipantOneId == request.RecipientId && c.ParticipantTwoId == senderId), ct);

        if (conversation == null)
        {
            // FR-MSG-11: store the pair in a CANONICAL order (smaller GUID first) so
            // the unique index on (ParticipantOneId, ParticipantTwoId) actually
            // enforces one-conversation-per-pair — otherwise a concurrent first
            // message from each side races two rows in with swapped columns. Reads
            // map participant→user by id, so the order carries no other meaning.
            var (participantOne, participantTwo) = senderId.CompareTo(request.RecipientId) <= 0
                ? (senderId, request.RecipientId)
                : (request.RecipientId, senderId);
            conversation = new ChatConversation
            {
                ParticipantOneId = participantOne,
                ParticipantTwoId = participantTwo,
            };
            db.Conversations.Add(conversation);
        }

        var recipientId = request.RecipientId;
        var isBlocked = await db.UserBlocks.AnyAsync(b =>
            (b.BlockerId == recipientId && b.BlockedUserId == currentUser.UserId) ||
            (b.BlockerId == currentUser.UserId && b.BlockedUserId == recipientId), ct);

        if (isBlocked)
        {
            throw new ConflictException("Cannot send message. User is blocked.");
        }

        // SEC-07 defense-in-depth: strip any HTML/script markup before persisting,
        // mirroring community posts/replies. The chat UI renders the body as a React
        // text node today, but the same string also fans out to a SignalR push and an
        // email notification preview (a non-React sink), so sanitizing on store keeps
        // every sink safe.
        var sanitizedBody = new Ganss.Xss.HtmlSanitizer().Sanitize(request.Body);

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Body = sanitizedBody,
            SentAt = DateTimeOffset.UtcNow
        };

        db.Messages.Add(message);

        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.SentAt;
        conversation.IsArchivedForParticipantOne = false;
        conversation.IsArchivedForParticipantTwo = false;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await chatNotifier.NotifyNewMessageAsync(conversation.Id, message.Id, message.SenderId, message.Body, message.SentAt, ct);

        // Fan out to the bell-icon + email notification for offline recipients —
        // the realtime push above only reaches subscribers already in the
        // conversation group, so anyone elsewhere in the app would otherwise
        // miss the message until they refresh the chat page.
        await publisher.Publish(
            new ChatMessageReceivedEvent(
                ConversationId: conversation.Id,
                MessageId: message.Id,
                SenderId: senderId,
                RecipientId: request.RecipientId,
                BodyPreview: sanitizedBody),
            ct).ConfigureAwait(false);

        return message.Id;
    }
}

