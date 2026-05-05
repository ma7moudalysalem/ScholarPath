using MediatR;
using ScholarPath.Application.Dashboard.DTOs;

namespace ScholarPath.Application.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery(Guid UserId) : IRequest<DashboardSummaryDto>;
