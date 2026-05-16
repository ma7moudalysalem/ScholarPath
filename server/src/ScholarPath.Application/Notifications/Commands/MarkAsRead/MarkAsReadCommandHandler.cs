using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Commands.MarkAsRead;

public sealed class MarkAsReadCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock)
    : IRequestHandler<MarkAsReadCommand>
{
    public async Task Handle(MarkAsReadCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.NotificationId, ct)
            ?? throw new NotFoundException(nameof(Notification), request.NotificationId);

        if (notification.RecipientUserId != userId)
            throw new ForbiddenAccessException("This notification belongs to another user.");

        // Idempotent — preserve the original ReadAt on repeat calls.
        if (notification.IsRead)
            return;

        notification.IsRead = true;
        notification.ReadAt = clock.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
