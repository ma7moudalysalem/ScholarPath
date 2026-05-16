using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Commands.MarkAllAsRead;

public sealed class MarkAllAsReadCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<MarkAllAsReadCommand, int>
{
    public async Task<int> Handle(MarkAllAsReadCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var now = clock.UtcNow;
        return await db.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                s => s.SetProperty(n => n.IsRead, true)
                      .SetProperty(n => n.ReadAt, (DateTimeOffset?)now),
                ct);
    }
}
