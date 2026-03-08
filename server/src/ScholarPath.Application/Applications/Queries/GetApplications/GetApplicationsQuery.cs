using MediatR;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Queries.GetApplications;

public record GetApplicationsQuery(
    Guid UserId,
    ApplicationStatus? Status,
    string SortBy,
    int Page,
    int PageSize
) : IRequest<PaginatedResponse<ApplicationListItemDto>>;
