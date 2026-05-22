using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Auth;

/// <summary>Builds the <see cref="AuthTokensDto"/> returned by register/login/refresh.</summary>
internal static class AuthDtoFactory
{
    public static AuthTokensDto Build(TokenPair tokens, ApplicationUser user, IReadOnlyList<string> roles) =>
        new(
            tokens.AccessToken,
            tokens.RefreshToken,
            tokens.AccessTokenExpiresAt,
            tokens.RefreshTokenExpiresAt,
            new CurrentUserDto(
                user.Id,
                user.Email ?? string.Empty,
                user.FirstName,
                user.LastName,
                user.FullName,
                user.ProfileImageUrl,
                user.AccountStatus.ToString(),
                user.IsOnboardingComplete,
                roles,
                user.ActiveRole,
                user.PreferredLanguage,
                // AUTH-CODE-06 — surface the most recent onboarding rejection
                // note so the wizard can render it on resubmission. Only set
                // when Profile is loaded (the auth/me query loads it; the
                // login/refresh paths don't, which is fine: those flows
                // immediately call /api/auth/me to hydrate the full user).
                user.Profile?.LastOnboardingRejectionReason,
                user.Profile?.LastOnboardingRejectedAt));
}
