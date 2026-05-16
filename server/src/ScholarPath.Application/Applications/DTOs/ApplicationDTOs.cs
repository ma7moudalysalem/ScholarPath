namespace ScholarPath.Application.Applications.DTOs;

public record CompanyApplicationRow(
    Guid ApplicationId,
    Guid StudentId,
    string StudentName,
    Guid ScholarshipId,
    string ScholarshipTitle,
    Domain.Enums.ApplicationStatus Status,
    DateTimeOffset? SubmittedAt);

public record CompanyApplicationDetailsDto(
    Guid ApplicationId,
    Guid StudentId,
    string StudentName,
    Guid ScholarshipId,
    string ScholarshipTitle,
    Domain.Enums.ApplicationStatus Status,
    DateTimeOffset? SubmittedAt,
    string? FormDataJson,
    string? AttachedDocumentsJson);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
