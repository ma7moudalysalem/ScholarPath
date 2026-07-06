using System;
using System.Linq;
using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Admin.Commands.SetUserStatus;

public sealed class SetUserStatusCommandHandler(
    IUserAdministration admin,
    ICurrentUserService currentUser,
    ILogger<SetUserStatusCommandHandler> logger)
    : IRequestHandler<SetUserStatusCommand, bool>
{
    public async Task<bool> Handle(SetUserStatusCommand request, CancellationToken ct)
    {
        // Privilege-tier guard (mirrors ChangeUserRoleCommandHandler): the
        // controller only requires Admin OR SuperAdmin, but suspending/deactivating
        // an account revokes all its sessions — a plain Admin must NOT be able to do
        // that to a peer Admin or a SuperAdmin (which could lock the owners out), and
        // no admin may change their OWN status.
        var currentUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated admin id is missing.");

        if (request.UserId == currentUserId)
        {
            throw new ForbiddenAccessException("You cannot change your own account status.");
        }

        var targetRoles = await admin.GetRolesAsync(request.UserId, ct).ConfigureAwait(false);
        var targetIsPrivileged = targetRoles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

        if (targetIsPrivileged && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException(
                "Only a SuperAdmin can change the status of an Admin or SuperAdmin account.");
        }

        var ok = await admin.SetAccountStatusAsync(
            request.UserId, request.NewStatus, request.Reason, ct).ConfigureAwait(false);

        if (!ok)
        {
            logger.LogWarning("SetUserStatus no-op: user {UserId} not found.", request.UserId);
            throw new NotFoundException("User", request.UserId);
        }

        logger.LogInformation("Admin set user {UserId} status → {Status} (reason: {Reason})",
            request.UserId, request.NewStatus, request.Reason ?? "<none>");

        return true;
    }
}
