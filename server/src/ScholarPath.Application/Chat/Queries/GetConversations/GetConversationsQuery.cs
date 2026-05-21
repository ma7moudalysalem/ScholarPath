using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Chat.DTOs;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Chat.Queries.GetConversations;

public sealed record GetConversationsQuery : IRequest<List<ChatConversationDto>>;

public sealed class GetConversationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IChatPresenceQuery presence)
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
            .Select(u => new { u.Id, u.FullName, u.ProfileImageUrl })
            .ToDictionaryAsync(u => u.Id, u => (u.FullName, u.ProfileImageUrl), ct);

        // Which of these participants the current user has blocked — drives the
        // Block / Unblock toggle in the chat UI (UAT TC-006).
        var blockedUserIds = (await db.UserBlocks
                .AsNoTracking()
                .Where(b => b.BlockerId == currentUserId && otherUserIds.Contains(b.BlockedUserId))
                .Select(b => b.BlockedUserId)
                .ToListAsync(ct)
                .ConfigureAwait(false))
            .ToHashSet();

        return conversations.Select(c =>
        {
            var other = otherUsers.TryGetValue(c.OtherParticipantId, out var u)
                ? u
                : (FullName: "Unknown User", ProfileImageUrl: (string?)null);
            return new ChatConversationDto(
                c.Id,
                c.OtherParticipantId,
                other.FullName ?? "Unknown User",
                other.ProfileImageUrl,
                c.LastMessageBody,
                c.LastMessageAt,
                presence.IsOnline(c.OtherParticipantId),
                blockedUserIds.Contains(c.OtherParticipantId));
        }).ToList();
    }
}
