namespace ScholarPath.Application.Chat.DTOs;

public record ChatConversationDto(
    Guid Id,
    Guid OtherParticipantId,
    string OtherParticipantName,
    string? OtherParticipantAvatarUrl,
    string? LastMessageBody,
    DateTimeOffset? LastMessageAt,
    bool IsOnline);
