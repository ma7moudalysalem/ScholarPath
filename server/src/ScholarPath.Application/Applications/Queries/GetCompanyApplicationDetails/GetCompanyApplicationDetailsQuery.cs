using MediatR;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Queries.GetScholarshipProviderApplicationDetails;

public sealed record GetScholarshipProviderApplicationDetailsQuery(
    Guid ApplicationId) : IRequest<ScholarshipProviderApplicationDetailsDto>;
