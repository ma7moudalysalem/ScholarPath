using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Entities;

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
    IChatRealtimeNotifier chatNotifier)
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
            conversation = new ChatConversation
            {
                ParticipantOneId = senderId,
                ParticipantTwoId = request.RecipientId,
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

        var message = new ChatMessage
        {
            ConversationId = conversation.Id,
            SenderId = senderId,
            Body = request.Body,
            SentAt = DateTimeOffset.UtcNow
        };

        db.Messages.Add(message);

        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.SentAt;
        conversation.IsArchivedForParticipantOne = false;
        conversation.IsArchivedForParticipantTwo = false;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await chatNotifier.NotifyNewMessageAsync(conversation.Id, message.Id, message.SenderId, message.Body, message.SentAt, ct);

        return message.Id;
    }
}

