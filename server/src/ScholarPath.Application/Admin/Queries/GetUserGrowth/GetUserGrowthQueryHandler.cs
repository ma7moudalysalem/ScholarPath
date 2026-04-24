using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Admin.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Queries.GetUserGrowth;

public sealed class GetUserGrowthQueryHandler(
    IApplicationDbContext db,
    IDateTimeService clock)
    : IRequestHandler<GetUserGrowthQuery, IReadOnlyList<GrowthPoint>>
{
    public async Task<IReadOnlyList<GrowthPoint>> Handle(GetUserGrowthQuery request, CancellationToken ct)
    {
        var days = Math.Clamp(request.Days, 7, 180);
        var startUtc = clock.UtcNow.Date.AddDays(-days + 1);

        var grouped = await db.Users
            .Where(u => u.CreatedAt >= startUtc)
            .GroupBy(u => u.CreatedAt.Date)
            .Select(g => new { Day = g.Key, Count = g.Count() })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var byDay = grouped.ToDictionary(g => g.Day, g => g.Count);

        // Fill zero-days so the chart has a continuous series
        var result = new List<GrowthPoint>(days);
        for (var i = 0; i < days; i++)
        {
            var d = startUtc.AddDays(i);
            result.Add(new GrowthPoint(new DateTimeOffset(d, TimeSpan.Zero),
                byDay.TryGetValue(d, out var c) ? c : 0));
        }

        return result;
    }
}
