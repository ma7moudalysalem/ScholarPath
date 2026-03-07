using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.UpgradeRequests.DTOs;

public record UpgradeRequestDetailDto(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserName,
    UserRole RequestedRole,
    UpgradeRequestStatus Status,
    string? AdminNotes,
    string? RejectionReason,
    string? RejectionReasons,
    string? ReviewedBy,
    DateTime? ReviewedAt,
    DateTime CreatedAt,
    // Consultant fields
    string? ExperienceSummary,
    List<EducationEntryDto>? Education,
    List<string>? ExpertiseTags,
    List<string>? Languages,
    List<UpgradeRequestLinkDto>? Links,
    List<UpgradeRequestFileDto>? Files,
    // Company fields
    string? CompanyName,
    string? CompanyCountry,
    string? CompanyWebsite,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactPhone,
    string? CompanyRegistrationNumber);

public record UpgradeRequestFileDto(
    Guid Id,
    string FileName,
    string ContentType,
    long FileSize,
    DateTime UploadedAt);

public record ResubmitUpgradeRequest(
    List<EducationEntryDto>? Education,
    string? ExperienceSummary,
    List<string>? ExpertiseTags,
    List<string>? Languages,
    List<UpgradeRequestLinkDto>? Links,
    // Company
    string? CompanyName,
    string? Country,
    string? Website,
    string? ContactPersonName,
    string? ContactEmail,
    string? ContactPhone,
    string? CompanyRegistrationNumber);
