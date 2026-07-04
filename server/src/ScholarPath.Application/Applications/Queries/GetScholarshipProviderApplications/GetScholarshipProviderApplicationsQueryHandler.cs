using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetScholarshipProviderApplications;

public sealed class GetScholarshipProviderApplicationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipProviderApplicationsQuery, PagedResult<ScholarshipProviderApplicationRow>>
{
    public async Task<PagedResult<ScholarshipProviderApplicationRow>> Handle(GetScholarshipProviderApplicationsQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var q = db.Applications
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Scholarship)
            // A Draft is created the instant a student clicks Apply (before filling
            // in / submitting). Providers must only see SUBMITTED applications —
            // never a student's unsubmitted in-progress form + documents.
            .Where(a => a.Scholarship != null
                        && a.Scholarship.OwnerScholarshipProviderId == currentUser.UserId
                        && a.Status != ApplicationStatus.Draft);

        if (request.ScholarshipId.HasValue)
        {
            q = q.Where(a => a.ScholarshipId == request.ScholarshipId.Value);
        }

        if (request.Status.HasValue)
        {
            q = q.Where(a => a.Status == request.Status.Value);
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);

        var rows = await q
            .OrderByDescending(a => a.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new ScholarshipProviderApplicationRow(
                a.Id,
                a.StudentId,
                a.Student != null ? a.Student.FullName : "Unknown",
                // Guarded by the WHERE above: Scholarship is non-null so ScholarshipId is set.
                a.ScholarshipId ?? Guid.Empty,
                a.Scholarship != null ? a.Scholarship.TitleEn : "Unknown",
                a.Status,
                a.SubmittedAt))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<ScholarshipProviderApplicationRow>(rows, page, pageSize, total);
    }
}
