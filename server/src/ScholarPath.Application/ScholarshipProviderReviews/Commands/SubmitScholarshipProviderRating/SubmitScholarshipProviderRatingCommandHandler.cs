using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ScholarshipProviderReviews.Commands.SubmitScholarshipProviderRating;

public sealed class SubmitScholarshipProviderRatingCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationDispatcher notifications,
    ILogger<SubmitScholarshipProviderRatingCommandHandler> logger)
    : IRequestHandler<SubmitScholarshipProviderRatingCommand, Guid>
{
    public async Task<Guid> Handle(SubmitScholarshipProviderRatingCommand request, CancellationToken ct)
    {
        var application = await db.Applications
            .Include(a => a.Scholarship)
            .FirstOrDefaultAsync(a => a.Id == request.ApplicationId, ct)
            ?? throw new NotFoundException(nameof(ApplicationTracker), request.ApplicationId);

        if (application.StudentId != currentUser.UserId)
        {
            throw new ForbiddenAccessException();
        }

        if (application.Status is not (ApplicationStatus.Accepted or ApplicationStatus.Rejected))
        {
            throw new ConflictException("Application must be in a final state to submit a review.");
        }

        // Resolve the rated company server-side from the scholarship owner —
        // never trust a client-supplied ScholarshipProviderId (defamation vector).
        var companyId = application.Scholarship?.OwnerScholarshipProviderId
            ?? throw new ConflictException("This application is not linked to a company.");

        var existingReview = await db.ScholarshipProviderReviews
            .AnyAsync(r => r.ApplicationTrackerId == request.ApplicationId, ct);

        if (existingReview)
        {
            throw new ConflictException("A review has already been submitted for this application.");
        }

        var review = new ScholarshipProviderReview
        {
            ApplicationTrackerId = request.ApplicationId,
            StudentId = application.StudentId,
            ScholarshipProviderId = companyId,
            Rating = request.Rating,
            Comment = request.Comment
        };

        db.ScholarshipProviderReviews.Add(review);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        await notifications.DispatchAsync(
            companyId,
            NotificationType.ScholarshipProviderRatingReceived,
            new NotificationParams { Count = request.Rating },
            null,
            null,
            ct);

        // PB-005R: recalculate the company's rating snapshot, then run the
        // low-rating policy check. Done after the new review row is persisted
        // so the aggregate query sees it. Aggregation excludes soft-deleted
        // and admin-hidden rows so a moderation action immediately reshapes
        // the next computed average.
        await RecalculateAndFlagLowRatingAsync(companyId, ct);

        logger.LogInformation("Student {StudentId} submitted a {Rating}-star rating for company {ScholarshipProviderId}",
            currentUser.UserId, request.Rating, companyId);

        return review.Id;
    }

    private async Task RecalculateAndFlagLowRatingAsync(Guid companyId, CancellationToken ct)
    {
        // Aggregate over visible reviews only. Soft-deleted rows are already
        // filtered by the ScholarshipProviderReview global query filter; the explicit
        // !IsHiddenByAdmin keeps admin-moderated rows out of the average.
        var visibleReviews = db.ScholarshipProviderReviews
            .AsNoTracking()
            .Where(r => r.ScholarshipProviderId == companyId && !r.IsHiddenByAdmin);

        var reviewCount = await visibleReviews.CountAsync(ct);
        // Math.Round on the decimal average — server-side aggregation already
        // returns decimal, but a CLR Math.Round at write time keeps the
        // persisted snapshot at 2-decimal precision matching the column.
        decimal? averageRating = reviewCount == 0
            ? null
            : await visibleReviews.AverageAsync(r => (decimal)r.Rating, ct);
        if (averageRating is not null)
        {
            averageRating = Math.Round(averageRating.Value, 2, MidpointRounding.AwayFromZero);
        }

        var profile = await db.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == companyId, ct);

        if (profile is null)
        {
            // Onboarding always creates a UserProfile, so a missing one is a
            // data-integrity surprise rather than a normal path. Log and
            // exit — the review is already saved, no point throwing now.
            logger.LogWarning(
                "UserProfile not found for company {ScholarshipProviderId} — rating snapshot was not updated.",
                companyId);
            return;
        }

        // The displayed reputation snapshot stays ALL-TIME (overall standing).
        profile.ScholarshipProviderAverageRating = averageRating;
        profile.ScholarshipProviderReviewCount = reviewCount;

        // FR-APP-35: the low-rating FLAG decision is scoped to the trailing
        // 3 months only — "if the ScholarshipProvider average rating during the last 3
        // months falls below 2.5, flag for Admin assessment." A provider that
        // has recently improved should not stay flagged on old ratings, and a
        // provider that recently declined should be caught even if its all-time
        // average is still healthy.
        var windowStart = DateTimeOffset.UtcNow.AddMonths(-ScholarshipProviderRatingThresholds.FlaggingWindowMonths);
        var recentReviews = db.ScholarshipProviderReviews
            .AsNoTracking()
            .Where(r => r.ScholarshipProviderId == companyId
                        && !r.IsHiddenByAdmin
                        && r.CreatedAt >= windowStart);

        var recentCount = await recentReviews.CountAsync(ct);
        decimal? recentAverage = recentCount == 0
            ? null
            : await recentReviews.AverageAsync(r => (decimal)r.Rating, ct);
        if (recentAverage is not null)
        {
            recentAverage = Math.Round(recentAverage.Value, 2, MidpointRounding.AwayFromZero);
        }

        var shouldFlag = recentAverage is { } avg
            && recentCount >= ScholarshipProviderRatingThresholds.MinimumReviewsForFlagging
            && avg < ScholarshipProviderRatingThresholds.LowRatingThreshold;

        // Sticky flag: don't overwrite an existing FlaggedAt timestamp on
        // subsequent sub-2.5 ratings. The original flag-time is what the
        // admin queue surfaces; it's cleared only by an admin action.
        var firstFlagging = shouldFlag && profile.ScholarshipProviderLowRatingFlaggedAt is null;
        if (firstFlagging)
        {
            profile.ScholarshipProviderLowRatingFlaggedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        if (firstFlagging)
        {
            // Notify with the trailing-window figures that actually triggered
            // the flag, not the all-time snapshot.
            await NotifyAdminsAsync(companyId, recentAverage!.Value, recentCount, ct);
        }
    }

    private async Task NotifyAdminsAsync(
        Guid companyId,
        decimal averageRating,
        int reviewCount,
        CancellationToken ct)
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

            // Resolve a human-readable ScholarshipProvider name (best-effort — fall back to
            // a placeholder so a profile without FirstName/LastName doesn't
            // throw inside the notification template).
            var companyName = await db.Users
                .Where(u => u.Id == companyId)
                .Select(u => (u.FirstName + " " + u.LastName).Trim())
                .FirstOrDefaultAsync(ct);

            // AmountText carries the formatted average, Count carries the
            // review count — both fields already exist on NotificationParams,
            // no schema growth needed.
            var parameters = new NotificationParams
            {
                CounterpartyName = string.IsNullOrWhiteSpace(companyName) ? "A company" : companyName,
                AmountText = averageRating.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Count = reviewCount,
            };

            foreach (var adminId in adminIds)
            {
                // Per-admin idempotency key so re-running the handler (e.g.
                // during retries) never double-notifies the same admin about
                // the same flag event.
                var idempotencyKey = $"crr-low-rating:{companyId:N}:{adminId:N}";

                try
                {
                    await notifications.DispatchAsync(
                        adminId,
                        NotificationType.ScholarshipProviderLowRatingFlagged,
                        parameters,
                        deepLink: $"/admin/low-rated-companies",
                        idempotencyKey: idempotencyKey,
                        ct);
                }
                catch (Exception ex)
                {
                    // Notification dispatch must not roll back the rating
                    // submission or the snapshot update; log and continue.
                    logger.LogWarning(ex,
                        "Failed to dispatch ScholarshipProviderLowRatingFlagged to admin {AdminId} for company {ScholarshipProviderId}.",
                        adminId, companyId);
                }
            }

            logger.LogInformation(
                "Flagged company {ScholarshipProviderId} for low rating: avg={Avg} count={Count}, notified {AdminCount} admins.",
                companyId, averageRating, reviewCount, adminIds.Count);
        }
        catch (Exception ex)
        {
            // Resolving admin IDs failed (e.g. DB hiccup). Snapshot + flag are
            // already saved; log and move on — the admin queue page still
            // shows the flagged company on next load.
            logger.LogWarning(ex,
                "Could not resolve admin recipients for ScholarshipProviderLowRatingFlagged on company {ScholarshipProviderId}.",
                companyId);
        }
    }
}
