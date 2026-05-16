using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.SwitchRole;

public sealed class SwitchRoleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAdministration userAdministration,
    ITokenService tokenService)
    : IRequestHandler<SwitchRoleCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(SwitchRoleCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var roles = await userAdministration.GetRolesAsync(userId, ct);
        if (!roles.Contains(request.TargetRole, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenAccessException($"You do not hold the '{request.TargetRole}' role.");

        user.ActiveRole = request.TargetRole;
        await db.SaveChangesAsync(ct);

        // Invalidate every existing refresh token before issuing the new pair —
        // a role switch must not leave sessions alive that still carry the old role.
        await tokenService.RevokeAllForUserAsync(userId, "Role switched", ct);

        // Issue a fresh pair so the JWT carries the new active_role claim.
        var tokens = tokenService.IssueTokens(user, roles, request.TargetRole, rememberMe: false);
        return AuthDtoFactory.Build(tokens, user, roles);
    }
}
