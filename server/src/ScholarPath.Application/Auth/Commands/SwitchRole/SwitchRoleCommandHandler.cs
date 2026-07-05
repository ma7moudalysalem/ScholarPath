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
    IConsultantEligibilityService consultantEligibility,
    ITokenService tokenService)
    : IRequestHandler<SwitchRoleCommand, AuthTokensDto>
{
    private const string ConsultantRole = "Consultant";

    public async Task<AuthTokensDto> Handle(SwitchRoleCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var roles = await userAdministration.GetRolesAsync(userId, ct);
        if (!roles.Contains(request.TargetRole, StringComparer.OrdinalIgnoreCase))
            throw new ForbiddenAccessException($"You do not hold the '{request.TargetRole}' role.");

        // Consultant is a privileged capability, not just a role row: holding the
        // Consultant role is necessary but NOT sufficient. Block the switch unless
        // the account is a verified/approved consultant — otherwise a stale or
        // out-of-band Consultant role would let a plain student act as a
        // consultant (create availability, appear in the marketplace).
        var canActAsConsultant = await consultantEligibility
            .CanActAsConsultantAsync(userId, roles, ct);
        if (string.Equals(request.TargetRole, ConsultantRole, StringComparison.OrdinalIgnoreCase)
            && !canActAsConsultant)
        {
            throw new ForbiddenAccessException(
                "You can't switch to the Consultant role. A consultant upgrade request must be "
                + "approved before consultant access is activated for your account.");
        }

        user.ActiveRole = request.TargetRole;
        await db.SaveChangesAsync(ct);

        // Invalidate every existing refresh token before issuing the new pair —
        // a role switch must not leave sessions alive that still carry the old role.
        await tokenService.RevokeAllForUserAsync(userId, "Role switched", ct);

        // Issue a fresh pair so the JWT carries the new active_role claim.
        var tokens = tokenService.IssueTokens(user, roles, request.TargetRole, rememberMe: false);
        return AuthDtoFactory.Build(tokens, user, roles, canActAsConsultant);
    }
}
