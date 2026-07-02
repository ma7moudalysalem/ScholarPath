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
                .ThenInclude(s => s!.OwnerScholarshipProvider)
            .Where(a => a.StudentId == studentId && !a.IsDeleted)
            .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
            .Select(a => new StudentApplicationRow(
                a.Id,
                a.ScholarshipId,
                // Free-text trackers don't have a linked Scholarship — fall back to
                // the user-supplied title; failing that, an empty placeholder.
                a.Scholarship == null
                    ? (a.ExternalTitle ?? string.Empty)
                    : (lang == "ar"
                        ? (a.Scholarship.TitleAr ?? a.Scholarship.TitleEn)
                        : (a.Scholarship.TitleEn ?? a.Scholarship.TitleAr)),
                a.Scholarship != null ? a.Scholarship.OwnerScholarshipProviderId : (Guid?)null,
                a.Scholarship != null && a.Scholarship.OwnerScholarshipProvider != null
                    ? a.Scholarship.OwnerScholarshipProvider.FullName
                    : a.ExternalProvider,
                a.Status,
                a.Mode,
                a.UpdatedAt ?? a.CreatedAt))
            .ToListAsync(ct);

        return applications;
    }
}
