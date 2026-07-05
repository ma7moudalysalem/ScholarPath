using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Services;

/// <summary>
/// Single source of truth for the consultant rating snapshot + penalty ledger
/// (PB-006R, FR-CBR-33..39). Every handler that changes a consultant's standing —
/// rating submit, validated no-show, false-report, late cancellation — funnels
/// through here so the penalized-average formula and the admin low-rating flag
/// never drift between call sites.
/// </summary>
public sealed class ConsultantRatingService(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<ConsultantRatingService> logger)
{
    /// <summary>
    /// Multiplies the consultant's persisted penalty factor (compounding) and then
    /// recomputes the snapshot. The factor lingers across future review recomputes,
    /// so the deduction persists proportionally. Multiplier examples:
    /// 0.80 (cancel &lt;24h), 0.60 (validated no-show), 0.30 (false report).
    /// </summary>
    public async Task ApplyPenaltyFactorAsync(Guid consultantId, decimal multiplier, CancellationToken ct)
    {
        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == consultantId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            logger.LogWarning(
                "UserProfile not found for consultant {ConsultantId} — penalty factor not applied.",
                consultantId);
            return;
        }

        // Guard against a corrupted zero/negative factor collapsing all future
        // ratings; treat anything non-positive as a fresh 1.0 baseline.
        var baseFactor = profile.ConsultantRatingPenaltyFactor <= 0m
            ? 1.0m
            : profile.ConsultantRatingPenaltyFactor;

        profile.ConsultantRatingPenaltyFactor = decimal.Round(baseFactor * multiplier, 4, MidpointRounding.AwayFromZero);

        logger.LogInformation(
            "Applied penalty factor {Multiplier} to consultant {ConsultantId} (new factor {Factor}).",
            multiplier, consultantId, profile.ConsultantRatingPenaltyFactor);

        await RecalculateSnapshotAsync(consultantId, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Recomputes <c>ConsultantAverageRating</c>/<c>ConsultantReviewCount</c> from the
    /// consultant's visible reviews and the persisted penalty factor, evaluates the
    /// sticky low-rating flag on the PENALIZED average, persists, and notifies admins
    /// on the first flag. Callers should apply any factor/status changes to the tracked
    /// context first, then call this last — it commits the whole unit of work.
    /// </summary>
    public async Task RecalculateSnapshotAsync(Guid consultantId, CancellationToken ct)
    {
        var visibleReviews = db.ConsultantReviews
            .AsNoTracking()
            .Where(r => r.ConsultantId == consultantId && !r.IsHiddenByAdmin && !r.IsDeleted);

        // Total visible reviews — the "N reviews" figure shown to users.
        var totalReviewCount = await visibleReviews.CountAsync(ct).ConfigureAwait(false);

        // FR-CBR-38: the rating + flag are computed from the most recent
        // RatingWindowSize reviews only ("latest 20 reviews"), newest first.
        var windowRatings = await visibleReviews
            .OrderByDescending(r => r.CreatedAt)
            .Take(ConsultantRatingThresholds.RatingWindowSize)
            .Select(r => (decimal)r.Rating)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var sampleCount = windowRatings.Count;
        decimal? rawAverage = sampleCount == 0
            ? null
            : windowRatings.Average();

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == consultantId, ct)
            .ConfigureAwait(false);

        if (profile is null)
        {
            logger.LogWarning(
                "UserProfile not found for consultant {ConsultantId} — rating snapshot not updated.",
                consultantId);
            return;
        }

        var factor = profile.ConsultantRatingPenaltyFactor <= 0m ? 1.0m : profile.ConsultantRatingPenaltyFactor;

        // Penalized, displayed average. Null when there are no visible reviews —
        // the factor is still persisted so it applies the moment the first review lands.
        decimal? penalized = rawAverage is null
            ? null
            : decimal.Round(Math.Clamp(rawAverage.Value * factor, 0m, 5m), 2, MidpointRounding.AwayFromZero);

        profile.ConsultantAverageRating = penalized;
        profile.ConsultantReviewCount = totalReviewCount;

        var shouldFlag = penalized is { } avg
            && sampleCount >= ConsultantRatingThresholds.MinimumReviewsForFlagging
            && avg < ConsultantRatingThresholds.LowRatingThreshold;

        // Sticky flag: the original flag time is what the admin queue surfaces; a
        // later sub-threshold recompute must not overwrite it — only an admin clears it.
        var firstFlagging = shouldFlag && profile.ConsultantLowRatingFlaggedAt is null;
        if (firstFlagging)
        {
            profile.ConsultantLowRatingFlaggedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (firstFlagging)
        {
            await NotifyAdminsAsync(consultantId, penalized!.Value, sampleCount, ct).ConfigureAwait(false);
        }
    }

    private async Task NotifyAdminsAsync(Guid consultantId, decimal average, int reviewCount, CancellationToken ct)
    {
        try
        {
            var adminIds = await db.Users
                .Where(u => (u.ActiveRole == "Admin" || u.ActiveRole == "SuperAdmin")
                            && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);

            if (adminIds.Count == 0) return;

            var consultantName = await db.Users
                .Where(u => u.Id == consultantId)
                .Select(u => (u.FirstName + " " + u.LastName).Trim())
                .FirstOrDefaultAsync(ct)
                .ConfigureAwait(false);

            var parameters = new NotificationParams
            {
                CounterpartyName = string.IsNullOrWhiteSpace(consultantName) ? "A consultant" : consultantName,
                AmountText = average.ToString("0.00", CultureInfo.InvariantCulture),
                Count = reviewCount,
            };

            foreach (var adminId in adminIds)
            {
                var idempotencyKey = $"cbr-low-rating:{consultantId:N}:{adminId:N}";
                try
                {
                    await notifications.DispatchAsync(
                        adminId,
                        NotificationType.ConsultantLowRatingFlagged,
                        parameters,
                        deepLink: "/admin/consultants",
                        idempotencyKey: idempotencyKey,
                        ct).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex,
                        "Failed to dispatch ConsultantLowRatingFlagged to admin {AdminId} for consultant {ConsultantId}.",
                        adminId, consultantId);
                }
            }

            logger.LogInformation(
                "Flagged consultant {ConsultantId} for low (penalized) rating: avg={Avg} count={Count}, notified {AdminCount} admins.",
                consultantId, average, reviewCount, adminIds.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Could not resolve admin recipients for ConsultantLowRatingFlagged on consultant {ConsultantId}.",
                consultantId);
        }
    }
}
