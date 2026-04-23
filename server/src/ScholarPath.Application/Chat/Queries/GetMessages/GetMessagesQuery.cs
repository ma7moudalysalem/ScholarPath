using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Chat.Queries.GetMessages;

public record ChatMessageDto(
    Guid Id,
    Guid SenderId,
    string Body,
    DateTimeOffset SentAt,
    DateTimeOffset? ReadAt);

public sealed record GetMessagesQuery(
    Guid ConversationId,
    int Limit = 50,
    DateTimeOffset? Before = null) : IRequest<List<ChatMessageDto>>;

public sealed class GetMessagesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMessagesQuery, List<ChatMessageDto>>
{
    public async Task<List<ChatMessageDto>> Handle(GetMessagesQuery request, CancellationToken ct)
    {
        var conversation = await db.Conversations
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct)
            ?? throw new NotFoundException(nameof(ChatConversation), request.ConversationId);

        if (conversation.ParticipantOneId != currentUser.UserId && conversation.ParticipantTwoId != currentUser.UserId)
            throw new ForbiddenAccessException();

        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId && !m.IsDeleted);

        if (request.Before.HasValue)
        {
            query = query.Where(m => m.SentAt < request.Before.Value);
        }

        var limit = Math.Clamp(request.Limit, 1, 100);

        var messages = await query
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .Select(m => new ChatMessageDto(
                m.Id,
                m.SenderId,
                m.Body,
                m.SentAt,
                m.ReadAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        // Reverse to return chronological order for the client to append
        messages.Reverse();

        return messages;
    }
}
