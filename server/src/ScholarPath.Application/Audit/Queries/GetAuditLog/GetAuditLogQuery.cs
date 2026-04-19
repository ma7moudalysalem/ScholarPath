using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Audit.Queries.GetAuditLog;

public sealed record GetAuditLogQuery(
    int Page = 1,
    int PageSize = 50,
    AuditAction? Action = null,
    string? TargetType = null,
    Guid? ActorUserId = null,
    Guid? TargetId = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Search = null) : IRequest<PagedResult<AuditLogDto>>;

public sealed class GetAuditLogQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetAuditLogQuery, PagedResult<AuditLogDto>>
{
    private const int MaxPageSize = 200;

    public async Task<PagedResult<AuditLogDto>> Handle(GetAuditLogQuery q, CancellationToken ct)
    {
        var page = Math.Max(1, q.Page);
        var size = Math.Clamp(q.PageSize, 1, MaxPageSize);

        var query = db.AuditLogs.AsNoTracking();

        if (q.Action is not null)    query = query.Where(a => a.Action == q.Action.Value);
        if (q.TargetType is not null) query = query.Where(a => a.TargetType == q.TargetType);
        if (q.ActorUserId is not null) query = query.Where(a => a.ActorUserId == q.ActorUserId.Value);
        if (q.TargetId is not null)    query = query.Where(a => a.TargetId == q.TargetId.Value);
        if (q.From is not null)        query = query.Where(a => a.OccurredAt >= q.From.Value);
        if (q.To is not null)          query = query.Where(a => a.OccurredAt <= q.To.Value);
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var s = q.Search.Trim();
            query = query.Where(a =>
                (a.Summary != null && a.Summary.Contains(s))
                || a.TargetType.Contains(s));
        }

        var total = await query.CountAsync(ct).ConfigureAwait(false);

        var items = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * size)
            .Take(size)
            .Select(a => new AuditLogDto(
                a.Id,
                a.ActorUserId,
                a.ActorUserId == null ? null : db.Users.Where(u => u.Id == a.ActorUserId).Select(u => u.Email).FirstOrDefault(),
                a.Action,
                a.TargetType,
                a.TargetId,
                a.Summary,
                a.IpAddress,
                a.CorrelationId,
                a.OccurredAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<AuditLogDto>(items, page, size, total);
    }
}
