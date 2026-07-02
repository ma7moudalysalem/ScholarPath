using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ScholarshipProviderReviewRequests.Common;

/// <summary>
/// Email/in-app dispatch wrapper used by every ScholarshipProviderReviewRequest handler.
/// Per spec PART 7: notification delivery failure must NOT block payment or
/// request status updates. This helper swallows + logs dispatch errors so the
/// caller can transition state first and notify after, without the transition
/// being rolled back if SMTP is down.
/// </summary>
internal static class SafeNotificationDispatcher
{
    public static async Task TryDispatchAsync(
        INotificationDispatcher notifications,
        ILogger logger,
        Guid recipientUserId,
        NotificationType type,
        NotificationParams parameters,
        string? deepLink,
        string idempotencyKey,
        CancellationToken ct)
    {
        try
        {
            await notifications.DispatchAsync(
                recipientUserId, type, parameters, deepLink, idempotencyKey, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Notification dispatch failed for recipient {RecipientId} type {Type} key {Key} — continuing.",
                recipientUserId, type, idempotencyKey);
        }
    }
}
