namespace ScholarPath.Application.Profile.DTOs;

/// <summary>Role-agnostic profile view — Student / Company / Consultant fields are flat.</summary>
public sealed record UserProfileDto(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? ProfileImageUrl,
    string AccountStatus,
    string? CountryOfResidence,
    string? PreferredLanguage,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? LinkedInUrl,
    string? WebsiteUrl,
    string? AcademicLevel,
    string? FieldOfStudy,
    string? CurrentInstitution,
    decimal? Gpa,
    string? GpaScale,
    string? OrganizationLegalName,
    string? OrganizationWebsite,
    string? OrganizationVerificationStatus,
    decimal? SessionFeeUsd,
    int? SessionDurationMinutes,
    // Consultant professional profile (CR-PROF-08)
    string? ProfessionalTitle,
    int? YearsOfExperience,
    IReadOnlyCollection<string>? ExpertiseTags,
    IReadOnlyCollection<string>? Languages,
    string? Timezone,
    int CompletenessPercent,
    // CR-PROF-06: lets the UI hide the change-password card for SSO-only users.
    bool HasPasswordCredential);

/// <summary>Partial-update payload — every field is optional (PATCH semantics).
/// Role, ActiveRole, AccountStatus, VerificationStatus and approval fields are
/// deliberately absent here so that a profile update can never escalate or
/// change a user's standing (CR-PROF-11 — defence against mass-assignment).</summary>
public sealed record UpdateProfileRequestDto(
    string? FirstName,
    string? LastName,
    string? CountryOfResidence,
    string? PreferredLanguage,
    string? Biography,
    DateOnly? DateOfBirth,
    string? Nationality,
    string? LinkedInUrl,
    string? WebsiteUrl,
    string? AcademicLevel,
    string? FieldOfStudy,
    string? CurrentInstitution,
    decimal? Gpa,
    string? GpaScale,
    string? OrganizationLegalName,
    string? OrganizationWebsite,
    decimal? SessionFeeUsd,
    int? SessionDurationMinutes,
    // Consultant professional fields (CR-PROF-08)
    string? ProfessionalTitle,
    int? YearsOfExperience,
    IReadOnlyCollection<string>? ExpertiseTags,
    IReadOnlyCollection<string>? Languages,
    string? Timezone);
