using System;
using System.Linq;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Admin.Commands.SoftDeleteUser;

public sealed class SoftDeleteUserCommandHandler(
    IUserAdministration admin,
    IApplicationDbContext context,
    IDateTimeService clock,
    ICurrentUserService currentUser,
    ILogger<SoftDeleteUserCommandHandler> logger)
    : IRequestHandler<SoftDeleteUserCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteUserCommand request, CancellationToken ct)
    {
        // Privilege-tier guard (mirrors ChangeUserRoleCommandHandler): deleting an
        // account revokes its sessions and locks it out — a plain Admin must NOT be
        // able to delete a peer Admin or a SuperAdmin, and no admin may delete
        // their OWN account here.
        var currentUserId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Authenticated admin id is missing.");

        if (request.UserId == currentUserId)
        {
            throw new ForbiddenAccessException("You cannot delete your own account.");
        }

        var targetRoles = await admin.GetRolesAsync(request.UserId, ct).ConfigureAwait(false);
        var targetIsPrivileged = targetRoles.Any(r =>
            string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(r, "SuperAdmin", StringComparison.OrdinalIgnoreCase));

        if (targetIsPrivileged && !currentUser.IsInRole("SuperAdmin"))
        {
            throw new ForbiddenAccessException(
                "Only a SuperAdmin can delete an Admin or SuperAdmin account.");
        }

        // BUG-05: block deletion while the target consultant still owes live
        // consulting sessions. Soft-delete deactivates the account and revokes
        // sessions, so an orphaned future booking would leave the student in an
        // empty room with an unreleased card hold. Require the admin to
        // cancel/refund those bookings first (each party can cancel via the
        // existing refund path). Deleting a plain student, or a consultant with
        // only past/terminal bookings, is unaffected.
        var now = clock.UtcNow;
        var activeBookings = await context.Bookings
            .CountAsync(
                b => b.ConsultantId == request.UserId
                     && b.ScheduledStartAt > now
                     && (b.Status == BookingStatus.Requested || b.Status == BookingStatus.Confirmed),
                ct)
            .ConfigureAwait(false);

        if (activeBookings > 0)
        {
            throw new ConflictException(
                $"User has {activeBookings} active (requested/confirmed) upcoming consultation booking(s). " +
                "Cancel and refund those bookings before deleting the account.");
        }

        var ok = await admin.SoftDeleteAsync(request.UserId, ct).ConfigureAwait(false);
        if (!ok)
        {
            throw new NotFoundException("User", request.UserId);
        }

        logger.LogInformation("Admin soft-deleted user {UserId}. Reason: {Reason}",
            request.UserId, request.Reason ?? "<none>");

        return true;
    }
}
