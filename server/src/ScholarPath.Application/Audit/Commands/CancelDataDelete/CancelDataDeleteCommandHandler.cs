using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Audit.Commands.CancelDataDelete;

public sealed class CancelDataDeleteCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<CancelDataDeleteCommand, bool>
{
    public async Task<bool> Handle(CancelDataDeleteCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var pending = await db.UserDataRequests
            .Where(r => r.UserId == userId
                && r.Type == UserDataRequestType.Delete
                && r.Status == UserDataRequestStatus.Pending)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (pending is null)
        {
            throw new NotFoundException("UserDataRequest", userId);
        }

        pending.Status = UserDataRequestStatus.Cancelled;
        pending.CancelledAt = clock.UtcNow;
        pending.UpdatedAt = clock.UtcNow;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return true;
    }
}
