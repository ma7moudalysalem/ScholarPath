using MediatR;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetCompanyApplications;

public sealed record GetCompanyApplicationsQuery(
    Guid? ScholarshipId = null,
    int Page = 1,
    int PageSize = 25,
    ApplicationStatus? Status = null) : IRequest<PagedResult<CompanyApplicationRow>>;
