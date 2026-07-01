using ScholarPath.Application.Auth.Commands.SelectRole;

namespace ScholarPath.Application.Auth.DTOs;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset RefreshTokenExpiresAt,
    CurrentUserDto User);

public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    string? ProfileImageUrl,
    string AccountStatus,
    bool IsOnboardingComplete,
    IReadOnlyList<string> Roles,
    string? ActiveRole,
    string? PreferredLanguage,
    // AUTH-CODE-06 / FR-ONB-07 — the latest admin rejection note, so a
    // rejected applicant sees why their previous onboarding was rejected
    // before resubmitting. Null when the account has never been rejected
    // or when the most recent decision was an approval.
    string? LastOnboardingRejectionReason = null,
    DateTimeOffset? LastOnboardingRejectedAt = null,
    // Consultant eligibility gate — true only when the backend confirms the
    // user may act as a Consultant (verified/approved), NOT merely that a
    // Consultant role row exists. The role-switch UI uses this so a stale or
    // unapproved Consultant role can never surface the "switch to Consultant"
    // option; the backend still enforces the same rule server-side.
    bool CanActAsConsultant = false);

public sealed record RegisterRequestDto(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequestDto(string Email, string Password, bool RememberMe);
public sealed record RefreshTokenRequestDto(string RefreshToken);
public sealed record ForgotPasswordRequestDto(string Email);
public sealed record ResetPasswordRequestDto(string Token, string NewPassword);
public sealed record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);
public sealed record SwitchRoleRequestDto(string TargetRole);
public sealed record SelectRoleRequestDto(string Role, OnboardingDetails? Details);
public sealed record VerifyEmailRequestDto(Guid UserId, string Token);
public sealed record ResendVerificationRequestDto(string Email);
public sealed record RequestEmailChangeRequestDto(string NewEmail);
public sealed record ConfirmEmailChangeRequestDto(string NewEmail, string Token);
