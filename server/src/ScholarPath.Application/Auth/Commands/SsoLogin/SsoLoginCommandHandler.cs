using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.SsoLogin;

public sealed class SsoLoginCommandHandler(
    IApplicationDbContext db,
    ISsoService ssoService,
    ITokenService tokenService,
    IUserAdministration userAdministration,
    IDateTimeService clock)
    : IRequestHandler<SsoLoginCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(SsoLoginCommand request, CancellationToken ct)
    {
        var info = request.Provider.ToUpperInvariant() switch
        {
            "GOOGLE" => await ssoService.ExchangeGoogleCodeAsync(request.Code, request.RedirectUri, ct),
            "MICROSOFT" => await ssoService.ExchangeMicrosoftCodeAsync(request.Code, request.RedirectUri, ct),
            _ => throw new ConflictException($"Unsupported SSO provider '{request.Provider}'."),
        };

        var email = info.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var now = clock.UtcNow;

        var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, ct);

        IReadOnlyList<string> roles;
        if (user is null)
        {
            // First SSO sign-in — provision an Unassigned account (email is provider-verified).
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                NormalizedEmail = normalizedEmail,
                UserName = email,
                NormalizedUserName = normalizedEmail,
                FirstName = info.FirstName ?? string.Empty,
                LastName = info.LastName ?? string.Empty,
                ProfileImageUrl = info.ProfileImageUrl,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                AccountStatus = AccountStatus.Unassigned,
                IsOnboardingComplete = false,
                EmailConfirmed = true,
                CreatedAt = now,
                LastLoginAt = now,
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(ct);
            roles = [];
        }
        else
        {
            user.LastLoginAt = now;
            await db.SaveChangesAsync(ct);
            roles = await userAdministration.GetRolesAsync(user.Id, ct);
        }

        var tokens = tokenService.IssueTokens(user, roles, user.ActiveRole, rememberMe: false);
        return AuthDtoFactory.Build(tokens, user, roles);
    }
}
