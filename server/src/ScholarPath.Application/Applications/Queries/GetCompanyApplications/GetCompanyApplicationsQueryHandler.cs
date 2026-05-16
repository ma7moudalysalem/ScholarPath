using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Queries.GetCompanyApplications;

public sealed class GetCompanyApplicationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCompanyApplicationsQuery, PagedResult<CompanyApplicationRow>>
{
    public async Task<PagedResult<CompanyApplicationRow>> Handle(GetCompanyApplicationsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var q = db.Applications
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Scholarship)
            .Where(a => a.Scholarship != null && a.Scholarship.OwnerCompanyId == currentUser.UserId);

        if (request.ScholarshipId.HasValue)
        {
            q = q.Where(a => a.ScholarshipId == request.ScholarshipId.Value);
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new CompanyApplicationRow(
                a.Id,
                a.StudentId,
                a.Student != null ? a.Student.FullName : "Unknown",
                a.ScholarshipId,
                a.Scholarship != null ? a.Scholarship.TitleEn : "Unknown",
                a.Status,
                a.SubmittedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<CompanyApplicationRow>(rows, page, pageSize, total);
    }
}
