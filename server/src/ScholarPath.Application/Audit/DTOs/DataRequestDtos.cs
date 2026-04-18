using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.DTOs;

public sealed record DataRequestDto(
    Guid Id,
    UserDataRequestType Type,
    UserDataRequestStatus Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ScheduledProcessAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? CancelledAt,
    string? DownloadUrl,
    DateTimeOffset? DownloadExpiresAt);
