using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetUpgradeQueue;

public sealed class GetUpgradeQueueQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetUpgradeQueueQuery, PagedResult<UpgradeRequestRow>>
{
    public async Task<PagedResult<UpgradeRequestRow>> Handle(GetUpgradeQueueQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var q = db.UpgradeRequests.AsNoTracking();
        if (request.Status.HasValue)
        {
            q = q.Where(r => r.Status == request.Status.Value);
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var rows = await (
            from r in q
            join u in db.Users on r.UserId equals u.Id
            orderby r.CreatedAt descending
            select new UpgradeRequestRow(
                r.Id,
                r.UserId,
                u.Email!,
                r.Target,
                r.Status,
                r.Reason,
                r.CreatedAt))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<UpgradeRequestRow>(rows, page, pageSize, total);
    }
}
