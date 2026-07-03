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

    // The client's PagedResult<T> reads `totalCount`; expose it as an alias of
    // Total so the JSON wire shape matches. Without this the admin lists that page
    // through this record showed "Page 1 of NaN" because data.totalCount was
    // undefined on the wire (the field was serialized only as `total`).
    public int TotalCount => Total;
}
