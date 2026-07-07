using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetScholarshipProviderApplicationStatusCounts;

public sealed class GetScholarshipProviderApplicationStatusCountsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetScholarshipProviderApplicationStatusCountsQuery, ScholarshipProviderApplicationStatusCountsDto>
{
    public async Task<ScholarshipProviderApplicationStatusCountsDto> Handle(
        GetScholarshipProviderApplicationStatusCountsQuery request, CancellationToken ct)
    {
        // Same visibility rule as the paged list: only SUBMITTED (non-Draft)
        // applications for scholarships this provider owns. Grouped in SQL so the
        // whole set is aggregated without materialising every row.
        var grouped = await db.Applications
            .AsNoTracking()
            .Where(a => a.Scholarship != null
                        && a.Scholarship.OwnerScholarshipProviderId == currentUser.UserId
                        && a.Status != ApplicationStatus.Draft)
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byStatus = grouped.ToDictionary(x => x.Status.ToString(), x => x.Count);
        var total = grouped.Sum(x => x.Count);

        return new ScholarshipProviderApplicationStatusCountsDto(total, byStatus);
    }
}
