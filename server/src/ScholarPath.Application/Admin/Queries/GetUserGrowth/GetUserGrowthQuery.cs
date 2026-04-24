using MediatR;
using ScholarPath.Application.Admin.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetUserGrowth;

/// <summary>
/// Daily new-user count over the last N days (bounded 7..180, default 30)
/// for the admin growth chart.
/// </summary>
public sealed record GetUserGrowthQuery(int Days = 30) : IRequest<IReadOnlyList<GrowthPoint>>;
