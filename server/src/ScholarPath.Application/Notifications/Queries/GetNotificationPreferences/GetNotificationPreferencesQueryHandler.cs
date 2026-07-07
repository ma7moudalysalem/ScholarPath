using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Application.Notifications.DTOs;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Notifications.Queries.GetNotificationPreferences;

public sealed class GetNotificationPreferencesQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationPreferencesQuery, NotificationPreferencesDto>
{
    public async Task<NotificationPreferencesDto> Handle(
        GetNotificationPreferencesQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // Stored rows override the default; everything else is enabled by default,
        // mirroring how NotificationDispatcher.ResolveChannelsAsync gates channels.
        var stored = await db.NotificationPreferences
            .Where(p => p.UserId == userId)
            .ToDictionaryAsync(p => (p.Type, p.Channel), p => p.IsEnabled, ct);

        var preferences = new List<NotificationPreferenceDto>();
        foreach (var type in Enum.GetValues<NotificationType>())
        {
            // SystemTest is a user-fired diagnostic, not a delivery preference — never
            // surface it as a toggle row.
            if (type == NotificationType.SystemTest) continue;
            foreach (var channel in Enum.GetValues<NotificationChannel>())
            {
                var isEnabled = !stored.TryGetValue((type, channel), out var value) || value;
                preferences.Add(new NotificationPreferenceDto(
                    type.ToString(), channel.ToString(), isEnabled));
            }
        }

        var profile = await db.UserProfiles.AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        var settings = new NotificationSettingsDto(
            Muted: profile?.NotificationsMuted ?? false,
            QuietHoursEnabled: profile?.QuietHoursEnabled ?? false,
            QuietStart: profile?.QuietHoursStart?.ToString("HH:mm"),
            QuietEnd: profile?.QuietHoursEnd?.ToString("HH:mm"),
            QuietTimezone: profile?.QuietHoursTimezone);

        return new NotificationPreferencesDto(preferences, settings);
    }
}
