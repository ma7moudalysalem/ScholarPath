using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext db,
    ITokenService tokenService,
    IUserAdministration userAdministration,
    IConsultantEligibilityService consultantEligibility)
    : IRequestHandler<RefreshTokenCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        // Resolve the owning user before rotation — TokenService revokes the row on rotate.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.RefreshToken)));
        var tokenRow = await db.RefreshTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, ct);

        var user = tokenRow?.User
            ?? throw new ConflictException("Invalid or expired refresh token.");

        var tokens = await tokenService.RotateRefreshTokenAsync(
            request.RefreshToken, request.IpAddress, request.UserAgent, ct)
            ?? throw new ConflictException("Invalid or expired refresh token.");

        var roles = await userAdministration.GetRolesAsync(user.Id, ct);
        var canActAsConsultant = await consultantEligibility.CanActAsConsultantAsync(user.Id, roles, ct);
        return AuthDtoFactory.Build(tokens, user, roles, canActAsConsultant);
    }
}
