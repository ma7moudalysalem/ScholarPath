namespace ScholarPath.Application.Chat.DTOs;

public record ChatConversationDto(
    Guid Id,
    Guid OtherParticipantId,
    string OtherParticipantName,
    string? OtherParticipantAvatarUrl,
    string? LastMessageBody,
    DateTimeOffset? LastMessageAt,
    bool IsOnline,
    bool IsBlocked);

/// <summary>
/// A user the current user is allowed to start a direct-message conversation
/// with — surfaced by the compose user-picker.
/// </summary>
public record ChatContactDto(
    Guid Id,
    string Name,
    string? PhotoUrl,
    string? Role);
