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
    string? PreferredLanguage);

public sealed record RegisterRequestDto(string Email, string Password, string FirstName, string LastName);
public sealed record LoginRequestDto(string Email, string Password, bool RememberMe);
public sealed record RefreshTokenRequestDto(string RefreshToken);
public sealed record ForgotPasswordRequestDto(string Email);
public sealed record ResetPasswordRequestDto(string Token, string NewPassword);
public sealed record ChangePasswordRequestDto(string CurrentPassword, string NewPassword);
public sealed record SwitchRoleRequestDto(string TargetRole);
