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
    ILogger<SoftDeleteUserCommandHandler> logger)
    : IRequestHandler<SoftDeleteUserCommand, bool>
{
    public async Task<bool> Handle(SoftDeleteUserCommand request, CancellationToken ct)
    {
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
