using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real notification dispatcher (Task 5B). Renders text via the catalog, persists a
/// Notification row per enabled channel, pushes InApp over SignalR, and sends Email.
/// Channel-delivery failures are recorded on the row — they never fail the caller.
/// </summary>
public sealed class NotificationDispatcher(
    IApplicationDbContext db,
    INotificationCatalog catalog,
    IHubContext<NotificationHub> hub,
    IEmailService emailService,
    ILogger<NotificationDispatcher> logger) : INotificationDispatcher
{
    public async Task DispatchAsync(
        Guid recipientUserId, NotificationType type, NotificationParams parameters,
        string? deepLink, string? idempotencyKey, CancellationToken ct)
    {
        // Idempotency — Stripe/event replays must not double-insert.
        if (!string.IsNullOrEmpty(idempotencyKey)
            && await db.Notifications.AnyAsync(n => n.IdempotencyKey == idempotencyKey, ct))
        {
            return;
        }

        var content = catalog.Render(type, parameters);
        var channels = await ResolveChannelsAsync(recipientUserId, type, ct);

        var rows = new List<Notification>();
        foreach (var channel in channels)
        {
            var row = new Notification
            {
                Id = Guid.NewGuid(),
                RecipientUserId = recipientUserId,
                Type = type,
                Channel = channel,
                TitleEn = Truncate(content.TitleEn, 300),
                TitleAr = Truncate(content.TitleAr, 300),
                BodyEn = Truncate(content.BodyEn, 2000),
                BodyAr = Truncate(content.BodyAr, 2000),
                DeepLink = deepLink,
                MetadataJson = content.MetadataJson,
                Priority = 1,
                IdempotencyKey = idempotencyKey,
            };
            db.Notifications.Add(row);
            rows.Add(row);
        }

        try
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (ScholarPath.Application.Common.Exceptions.ConflictException)
            when (!string.IsNullOrEmpty(idempotencyKey))
        {
            // A concurrent dispatch with the same idempotency key won the race and
            // inserted these rows first — the unique (IdempotencyKey, Channel) index
            // rejected ours. Detach our rejected rows and no-op (idempotent).
            db.Notifications.RemoveRange(rows);
            return;
        }

        foreach (var row in rows)
            await DeliverAsync(row, content, ct);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DispatchBroadcastAsync(
        IReadOnlyCollection<Guid> recipientUserIds, NotificationType type,
        NotificationParams parameters, CancellationToken ct)
    {
        foreach (var userId in recipientUserIds.Distinct())
            await DispatchAsync(userId, type, parameters, deepLink: null, idempotencyKey: null, ct);
    }

    // InApp is always on; Email follows the user's preferences; Push has no delivery yet.
    private async Task<IReadOnlyList<NotificationChannel>> ResolveChannelsAsync(
        Guid userId, NotificationType type, CancellationToken ct)
    {
        var disabled = await db.NotificationPreferences
            .Where(p => p.UserId == userId && p.Type == type && !p.IsEnabled)
            .Select(p => p.Channel)
            .ToListAsync(ct);

        var channels = new List<NotificationChannel> { NotificationChannel.InApp };
        if (!disabled.Contains(NotificationChannel.Email))
            channels.Add(NotificationChannel.Email);
        return channels;
    }

    private async Task DeliverAsync(Notification row, NotificationContent content, CancellationToken ct)
    {
        try
        {
            switch (row.Channel)
            {
                case NotificationChannel.InApp:
                    await hub.Clients.Group($"user:{row.RecipientUserId}")
                        .SendAsync("notification", new
                        {
                            id = row.Id,
                            type = row.Type.ToString(),
                            row.TitleEn,
                            row.TitleAr,
                            row.BodyEn,
                            row.BodyAr,
                            row.DeepLink,
                            createdAt = DateTimeOffset.UtcNow,
                        }, ct);
                    break;

                case NotificationChannel.Email:
                    var recipient = await db.Users
                        .Where(u => u.Id == row.RecipientUserId)
                        .Select(u => new { u.Email, u.PreferredLanguage })
                        .FirstOrDefaultAsync(ct);
                    if (recipient is not null && !string.IsNullOrEmpty(recipient.Email))
                    {
                        // Render in the recipient's preferred language so AR users get
                        // AR copy and EN users get EN copy. Defaults to EN when the
                        // preference is unset or unrecognised.
                        var prefersArabic = !string.IsNullOrEmpty(recipient.PreferredLanguage)
                            && recipient.PreferredLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase);
                        var subject = prefersArabic ? content.TitleAr : content.TitleEn;
                        var body = prefersArabic ? content.BodyAr : content.BodyEn;
                        await emailService.SendAsync(
                            new EmailMessage(recipient.Email, subject,
                                $"<p>{body}</p>", body), ct);
                    }
                    break;

                case NotificationChannel.Push:
                    // Push has no delivery channel yet (no device-token infrastructure).
                    break;
            }

            row.DispatchedAt = DateTimeOffset.UtcNow;
            row.DispatchSucceeded = true;
        }
        catch (Exception ex)
        {
            row.DispatchedAt = DateTimeOffset.UtcNow;
            row.DispatchSucceeded = false;
            row.DispatchError = Truncate(ex.Message, 2000);
            logger.LogWarning(ex,
                "Notification {NotificationId} delivery failed on channel {Channel}.",
                row.Id, row.Channel);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
