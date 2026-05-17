using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Commands.UpdateNotificationPreference;

public sealed class UpdateNotificationPreferenceCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateNotificationPreferenceCommand>
{
    public async Task Handle(UpdateNotificationPreferenceCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var existing = await db.NotificationPreferences
            .FirstOrDefaultAsync(
                p => p.UserId == userId
                  && p.Type == request.Type
                  && p.Channel == request.Channel,
                ct);

        if (existing is null)
        {
            // Only persist a row when it diverges from the enabled-by-default state —
            // an "enable" with no stored row is already the effective behaviour.
            if (request.IsEnabled)
                return;

            db.NotificationPreferences.Add(new NotificationPreference
            {
                UserId = userId,
                Type = request.Type,
                Channel = request.Channel,
                IsEnabled = false,
            });
        }
        else
        {
            existing.IsEnabled = request.IsEnabled;
        }

        await db.SaveChangesAsync(ct);
    }
}
