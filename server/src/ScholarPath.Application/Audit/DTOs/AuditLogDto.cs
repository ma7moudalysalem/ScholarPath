using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.DTOs;

public sealed record AuditLogDto(
    Guid Id,
    Guid? ActorUserId,
    string? ActorEmail,
    AuditAction Action,
    string TargetType,
    Guid? TargetId,
    string? Summary,
    string? IpAddress,
    string? CorrelationId,
    DateTimeOffset OccurredAt);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int Total)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)Total / PageSize);
}
