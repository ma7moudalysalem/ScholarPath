using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Application.Notifications.Commands.UpdateNotificationSettings;

/// <summary>
/// Updates the current user's global notification "do not disturb" settings
/// (FR-228): mute-all + quiet hours. Quiet times are "HH:mm" strings interpreted
/// in <paramref name="QuietTimezone"/> (the browser's IANA zone), so the server
/// evaluates the window in the user's local time.
/// </summary>
public sealed record UpdateNotificationSettingsCommand(
    bool Muted,
    bool QuietHoursEnabled,
    string? QuietStart,
    string? QuietEnd,
    string? QuietTimezone) : IRequest;

public sealed class UpdateNotificationSettingsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser) : IRequestHandler<UpdateNotificationSettingsCommand>
{
    public async Task Handle(UpdateNotificationSettingsCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var profile = await db.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException("UserProfile", userId);

        profile.NotificationsMuted = request.Muted;
        profile.QuietHoursEnabled = request.QuietHoursEnabled;
        profile.QuietHoursStart = ParseTime(request.QuietStart);
        profile.QuietHoursEnd = ParseTime(request.QuietEnd);
        profile.QuietHoursTimezone = string.IsNullOrWhiteSpace(request.QuietTimezone)
            ? null : request.QuietTimezone.Trim();

        await db.SaveChangesAsync(ct);
    }

    private static TimeOnly? ParseTime(string? hhmm) =>
        TimeOnly.TryParse(hhmm, out var t) ? t : null;
}
