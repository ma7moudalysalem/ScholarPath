using MediatR;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Queries.GetMyApplications;

public record GetMyApplicationsQuery() : IRequest<IReadOnlyList<StudentApplicationRow>>;

public record StudentApplicationRow(
    Guid ApplicationId,
    Guid ScholarshipId,
    string ScholarshipTitle,
    Guid? CompanyId,
    string? CompanyName,
    Domain.Enums.ApplicationStatus Status,
    Domain.Enums.ApplicationMode Mode,
    DateTimeOffset UpdatedAt);
