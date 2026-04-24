using MediatR;
using ScholarPath.Application.Admin.DTOs;

namespace ScholarPath.Application.Admin.Queries.GetAnalyticsOverview;

public sealed record GetAnalyticsOverviewQuery : IRequest<AnalyticsOverviewDto>;
