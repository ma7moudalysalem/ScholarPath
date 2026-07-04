using System;
using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Commands.ChangeUserRole;

public sealed class ChangeUserRoleCommandHandler(
    IUserAdministration admin,
    ICurrentUserService currentUser,
    ILogger<ChangeUserRoleCommandHandler> logger)
    : IRequestHandler<ChangeUserRoleCommand, bool>
{
    public async Task<bool> Handle(ChangeUserRoleCommand request, CancellationToken ct)
    {
        // Privilege-tier guard (prevents self-escalation): the controller only
        // requires Admin OR SuperAdmin, but granting/revoking the privileged roles
        // (Admin, SuperAdmin) must be SuperAdmin-only — otherwise a plain Admin could
        // mint itself (or a confederate) SuperAdmin. And no admin may change their OWN roles.
        var currentUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated admin id is missing.");

        var isPrivilegedRole =
            string.Equals(request.Role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(request.Role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);

        if (isPrivilegedRole && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException(
                "Only a SuperAdmin can grant or revoke the Admin or SuperAdmin role.");
        }

        if (request.UserId == currentUserId)
        {
            throw new ForbiddenAccessException("You cannot change your own roles.");
        }

        var ok = request.Operation switch
        {
            RoleOp.Add    => await admin.AddRoleAsync(request.UserId, request.Role, ct).ConfigureAwait(false),
            RoleOp.Remove => await admin.RemoveRoleAsync(request.UserId, request.Role, ct).ConfigureAwait(false),
            _             => false,
        };

        if (!ok)
        {
            var verb = request.Operation == RoleOp.Add ? "add" : "remove";
            throw new ConflictException(
                $"Could not {verb} role '{request.Role}' for user {request.UserId}.");
        }

        logger.LogInformation("Admin {Op} role {Role} on {UserId}",
            request.Operation, request.Role, request.UserId);
        return true;
    }
}
