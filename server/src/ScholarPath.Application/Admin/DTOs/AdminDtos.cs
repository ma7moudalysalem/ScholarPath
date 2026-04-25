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
    DateTimeOffset? LastLoginAt);

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
    string? RequestedRole);

public sealed record UpgradeRequestRow(
    Guid Id,
    Guid UserId,
    string UserEmail,
    UpgradeTarget Target,
    UpgradeRequestStatus Status,
    string? Reason,
    DateTimeOffset CreatedAt);

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
