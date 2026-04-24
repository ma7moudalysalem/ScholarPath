using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Chat.DTOs;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Chat.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<List<ChatConversationDto>>;

public sealed class GetConversationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetConversationsQuery, List<ChatConversationDto>>
{
    public async Task<List<ChatConversationDto>> Handle(GetConversationsQuery request, CancellationToken ct)
    {
        var currentUserId = currentUser.UserId;

        var conversations = await db.Conversations
            .AsNoTracking()
            .Where(c => c.ParticipantOneId == currentUserId || c.ParticipantTwoId == currentUserId)
            .OrderByDescending(c => c.LastMessageAt)
            .Select(c => new
            {
                c.Id,
                OtherParticipantId = c.ParticipantOneId == currentUserId ? c.ParticipantTwoId : c.ParticipantOneId,
                c.LastMessageAt,
                LastMessageBody = db.Messages
                    .Where(m => m.Id == c.LastMessageId)
                    .Select(m => m.Body)
                    .FirstOrDefault()
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var otherUserIds = conversations.Select(c => c.OtherParticipantId).Distinct().ToList();
        var otherUsers = await db.Users
            .AsNoTracking()
            .Where(u => otherUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return conversations.Select(c => new ChatConversationDto(
            c.Id,
            c.OtherParticipantId,
            otherUsers.GetValueOrDefault(c.OtherParticipantId) ?? "Unknown User",
            null, // AvatarUrl if available
            c.LastMessageBody,
            c.LastMessageAt,
            false // Presence dot placeholder
        )).ToList();
    }
}
