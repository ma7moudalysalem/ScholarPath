using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.DTOs;

public sealed record AdminUserRow(
    Guid Id,
    string Email,
    string FullName,
    AccountStatus AccountStatus,
    bool IsOnboardingComplete,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    bool IsAtRisk,              // PB-018 FR-270 — reverse-ETL flag from Power BI
    decimal? RiskScore,         // null when no UserRiskFlag row exists yet
    bool BookingIntakeSuspended); // FR-094 — consultant booking intake auto-suspended for low ratings

public sealed record AdminUserDetail(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? ProfileImageUrl,
    AccountStatus AccountStatus,
    bool IsOnboardingComplete,
    IReadOnlyList<string> Roles,
    string? ActiveRole,
    string? CountryOfResidence,
    string? PreferredLanguage,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    bool IsDeleted);

public sealed record OnboardingRequestRow(
    Guid UserId,
    string Email,
    string FullName,
    AccountStatus AccountStatus,
    DateTimeOffset CreatedAt,
    string? RequestedRole,
    // ── ScholarshipProvider profile snapshot ────────────────────────────────────────────
    string? OrganizationLegalName,
    string? OrganizationWebsite,
    string? OrganizationEmail,
    string? OrganizationCountry,
    string? ScholarshipProviderType,
    string? ScholarshipProviderDescription,
    string? OrganizationRegistrationNumber,
    string? OrganizationTaxNumber,
    string? ContactPersonFullName,
    string? ContactPersonPosition,
    string? ContactPhoneNumber,
    // ── Consultant profile snapshot ─────────────────────────────────────────
    string? Biography,
    string? ProfessionalTitle,
    string? HighestDegree,
    string? FieldOfExpertise,
    int? YearsOfExperience,
    decimal? SessionFeeUsd,
    int? SessionDurationMinutes,
    string? ExpertiseTagsJson,
    string? LanguagesJson,
    string? Timezone,
    string? LinkedInUrl,
    string? PortfolioUrl,
    string? ConsultantCountry);

public sealed record UpgradeRequestRow(
    Guid Id,
    Guid UserId,
    string UserEmail,
    UpgradeTarget Target,
    UpgradeRequestStatus Status,
    string? Reason,
    DateTimeOffset CreatedAt,
    // ── Applicant + proposed consultant profile snapshot ─────────────────────
    // A consultant upgrade grants a paid, earning role, so the reviewer must see
    // the profile the applicant proposed — not just a one-line reason. Mirrors
    // the OnboardingRequestRow consultant snapshot; verification documents are
    // fetched separately via the same onboarding-documents endpoint.
    string? FullName = null,
    string? Biography = null,
    string? ProfessionalTitle = null,
    string? HighestDegree = null,
    string? FieldOfExpertise = null,
    int? YearsOfExperience = null,
    decimal? SessionFeeUsd = null,
    int? SessionDurationMinutes = null,
    string? ExpertiseTagsJson = null,
    string? LanguagesJson = null,
    string? Timezone = null,
    string? LinkedInUrl = null,
    string? PortfolioUrl = null,
    string? ConsultantCountry = null);

public sealed record AnalyticsOverviewDto(
    int TotalUsers,
    int ActiveUsers,
    int PendingApprovals,
    int TotalScholarships,
    int OpenScholarships,
    int TotalApplications,
    int SubmittedApplications,
    int TotalBookings,
    int CompletedBookings,
    long RevenueCentsCaptured,
    long ProfitShareCentsAccumulated,
    int AiInteractions24h);

public sealed record GrowthPoint(DateTimeOffset Date, int Count);

public sealed record ApplicationStatusPoint(ApplicationStatus Status, int Count);

public sealed record RedactionAuditSampleRow(
    Guid Id,
    Guid AiInteractionId,
    Guid UserId,
    string? UserEmail,
    string RedactedPrompt,
    DateTimeOffset SampledAt,
    RedactionVerdict? Verdict,
    Guid? ReviewerUserId,
    DateTimeOffset? ReviewedAt);
