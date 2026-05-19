using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAdministration userAdministration,
    ITokenService tokenService,
    ILogger<SelectRoleCommandHandler> logger)
    : IRequestHandler<SelectRoleCommand, AuthTokensDto>
{
    public async Task<AuthTokensDto> Handle(SelectRoleCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        // Role selection is a one-time gate — only an Unassigned, role-less account qualifies.
        var existingRoles = await userAdministration.GetRolesAsync(userId, ct);
        if (existingRoles.Count > 0 || user.AccountStatus != AccountStatus.Unassigned)
            throw new ConflictException("A role has already been selected for this account.");

        if (request.Role == "Student")
        {
            // Students need no admin review — grant the role and activate immediately.
            await userAdministration.AddRoleAsync(userId, "Student", ct);
            user.ActiveRole = "Student";
            user.AccountStatus = AccountStatus.Active;
            user.IsOnboardingComplete = true;
        }
        else
        {
            // Company / Consultant must be vetted — park them in the onboarding queue.
            // ActiveRole carries the requested role (the queue surfaces it); the Identity
            // role itself is granted by ReviewOnboardingCommandHandler on approval.
            user.ActiveRole = request.Role;
            user.AccountStatus = AccountStatus.PendingApproval;

            // Persist the onboarding profile details so the admin reviews a
            // complete request, not a bare role pick.
            if (request.Details is { } details)
            {
                user.Profile ??= new UserProfile
                {
                    UserId = userId,
                    CreatedAt = DateTimeOffset.UtcNow,
                };

                if (request.Role == "Company")
                {
                    user.Profile.OrganizationLegalName = details.OrganizationLegalName;
                    user.Profile.OrganizationWebsite = details.OrganizationWebsite;
                    user.Profile.OrganizationVerificationStatus = "Pending";
                }
                else
                {
                    user.Profile.Biography = details.Biography;
                    user.Profile.SessionFeeUsd = details.SessionFeeUsd;
                    user.Profile.SessionDurationMinutes ??= 45;
                    user.Profile.ExpertiseTagsJson = details.ExpertiseTags is { Length: > 0 } tags
                        ? JsonSerializer.Serialize(tags)
                        : null;
                }
            }
        }

        await db.SaveChangesAsync(ct);

        // Kill any session still carrying the old role-less JWT.
        await tokenService.RevokeAllForUserAsync(userId, "Role selected", ct);

        var roles = await userAdministration.GetRolesAsync(userId, ct);
        var tokens = tokenService.IssueTokens(user, roles, user.ActiveRole, rememberMe: false);

        logger.LogInformation(
            "User {UserId} selected role {Role} -> account status {Status}.",
            userId, request.Role, user.AccountStatus);

        return AuthDtoFactory.Build(tokens, user, roles);
    }
}
