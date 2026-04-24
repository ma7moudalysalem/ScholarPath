using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.Application.Chat.Commands.SendMessage;

public sealed record SendMessageCommand(
    Guid ConversationId,
    string Body) : IRequest<Guid>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(v => v.ConversationId).NotEmpty();
        RuleFor(v => v.Body).NotEmpty().MaximumLength(2000);
    }
}

public sealed class SendMessageCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IHubContext<ChatHub> hubContext)
    : IRequestHandler<SendMessageCommand, Guid>
{
    public async Task<Guid> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(ChatConversation), request.ConversationId);

        if (conversation.ParticipantOneId != currentUser.UserId && conversation.ParticipantTwoId != currentUser.UserId)
            throw new ForbiddenAccessException();

        var recipientId = conversation.ParticipantOneId == currentUser.UserId 
            ? conversation.ParticipantTwoId 
            : conversation.ParticipantOneId;

        // T-008 - Block enforcement
        var isBlocked = await db.UserBlocks.AnyAsync(b => 
            (b.BlockerId == recipientId && b.BlockedUserId == currentUser.UserId) ||
            (b.BlockerId == currentUser.UserId && b.BlockedUserId == recipientId), ct);

        if (isBlocked)
        {
            throw new ConflictException("Cannot send message. User is blocked.");
        }

        var message = new ChatMessage
        {
            ConversationId = request.ConversationId,
            SenderId = currentUser.UserId,
            Body = request.Body,
            SentAt = DateTimeOffset.UtcNow
        };

        db.Messages.Add(message);

        conversation.LastMessageId = message.Id;
        conversation.LastMessageAt = message.SentAt;
        conversation.IsArchivedForParticipantOne = false;
        conversation.IsArchivedForParticipantTwo = false;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Broadcast to ChatHub
        await hubContext.Clients.Group($"conversation:{request.ConversationId}")
            .SendAsync("NewMessage", message.Id, message.ConversationId, message.SenderId, message.Body, message.SentAt, ct);

        // Notification fallback if offline - usually handled separately or by checking presence

        return message.Id;
    }
}
