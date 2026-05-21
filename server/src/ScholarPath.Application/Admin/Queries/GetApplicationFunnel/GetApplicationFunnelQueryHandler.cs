using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetApplicationFunnel;

public sealed class GetApplicationFunnelQueryHandler(IApplicationDbContext db)
    : IRequestHandler<GetApplicationFunnelQuery, IReadOnlyList<ApplicationStatusPoint>>
{
    public async Task<IReadOnlyList<ApplicationStatusPoint>> Handle(
        GetApplicationFunnelQuery request, CancellationToken ct)
    {
        // Exclude soft-deleted applications so the funnel mirrors user-visible state.
        var data = await db.Applications
            .Where(a => !a.IsDeleted)
            .GroupBy(a => a.Status)
            .Select(g => new ApplicationStatusPoint(g.Key, g.Count()))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return data;
    }
}
