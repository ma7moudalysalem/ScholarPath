using MediatR;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Queries.GetCompanyApplicationDetails;

public sealed record GetCompanyApplicationDetailsQuery(
    Guid ApplicationId) : IRequest<CompanyApplicationDetailsDto>;
