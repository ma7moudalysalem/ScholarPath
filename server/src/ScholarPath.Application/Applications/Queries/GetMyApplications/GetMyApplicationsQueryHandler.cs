using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Applications.Queries.GetMyApplications;

public sealed class GetMyApplicationsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetMyApplicationsQuery, IReadOnlyList<StudentApplicationRow>>
{
    public async Task<IReadOnlyList<StudentApplicationRow>> Handle(GetMyApplicationsQuery request, CancellationToken ct)
    {
        var studentId = currentUser.UserId;
        var lang = request.Language.ToLower() == "ar" ? "ar" : "en";

        var applications = await db.Applications
            .AsNoTracking()
            .Include(a => a.Scholarship)
                .ThenInclude(s => s!.OwnerCompany)
            .Where(a => a.StudentId == studentId && !a.IsDeleted)
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Select(a => new StudentApplicationRow(
                a.Id,
                a.ScholarshipId,
                lang == "ar"
                    ? (a.Scholarship!.TitleAr ?? a.Scholarship!.TitleEn)
                    : (a.Scholarship!.TitleEn ?? a.Scholarship!.TitleAr),
                a.Scholarship.OwnerCompanyId,
                a.Scholarship.OwnerCompany != null ? a.Scholarship.OwnerCompany.FullName : null,
                a.Status,
                a.Mode,
                a.UpdatedAt ?? a.CreatedAt))
            .ToListAsync(ct);

        return applications;
    }
}
