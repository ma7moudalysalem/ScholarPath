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
            join pj in db.UserProfiles on u.Id equals pj.UserId into pg
            from p in pg.DefaultIfEmpty()
            orderby r.CreatedAt descending
            select new UpgradeRequestRow(
                r.Id,
                r.UserId,
                u.Email!,
                r.Target,
                r.Status,
                r.Reason,
                r.CreatedAt,
                ((u.FirstName ?? "") + " " + (u.LastName ?? "")).Trim(),
                p != null ? p.Biography : null,
                p != null ? p.ProfessionalTitle : null,
                p != null ? p.HighestDegree : null,
                p != null ? p.FieldOfExpertise : null,
                p != null ? p.YearsOfExperience : null,
                p != null ? p.SessionFeeUsd : null,
                p != null ? p.SessionDurationMinutes : null,
                p != null ? p.ExpertiseTagsJson : null,
                p != null ? p.LanguagesJson : null,
                p != null ? p.Timezone : null,
                p != null ? p.LinkedInUrl : null,
                p != null ? p.PortfolioUrl : null,
                u.CountryOfResidence))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<UpgradeRequestRow>(rows, page, pageSize, total);
    }
}
