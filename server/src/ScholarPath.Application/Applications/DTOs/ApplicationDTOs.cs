namespace ScholarPath.Application.Applications.DTOs;

public record ScholarshipProviderApplicationRow(
    Guid ApplicationId,
    Guid StudentId,
    string StudentName,
    Guid ScholarshipId,
    string ScholarshipTitle,
    Domain.Enums.ApplicationStatus Status,
    DateTimeOffset? SubmittedAt);

/// <summary>A document attached to an application, visible to the company reviewer.</summary>
public record ScholarshipProviderDocumentInfo(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes);

public record ScholarshipProviderApplicationDetailsDto(
    Guid ApplicationId,
    Guid StudentId,
    string StudentName,
    Guid ScholarshipId,
    string ScholarshipTitle,
    Domain.Enums.ApplicationStatus Status,
    DateTimeOffset? SubmittedAt,
    string? FormDataJson,
    string? AttachedDocumentsJson,
    IReadOnlyList<ScholarshipProviderDocumentInfo> Documents);

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
