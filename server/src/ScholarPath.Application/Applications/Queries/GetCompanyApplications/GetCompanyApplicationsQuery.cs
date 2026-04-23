using MediatR;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Queries.GetCompanyApplications;

public sealed record GetCompanyApplicationsQuery(
    Guid? ScholarshipId = null,
    int Page = 1,
    int PageSize = 25) : IRequest<PagedResult<CompanyApplicationRow>>;
