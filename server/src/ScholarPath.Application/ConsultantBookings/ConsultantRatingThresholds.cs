namespace ScholarPath.Application.ConsultantBookings;

/// <summary>
/// Tunable thresholds + penalty factors for the Consultant rating/penalty policy
/// (PB-006R, FR-CBR-15..39). Mirrors <c>ScholarshipProviderRatingThresholds</c> but
/// adds the reputation-deduction multipliers.
///
/// <para>
/// The consultant's <c>UserProfile.ConsultantAverageRating</c> is the DISPLAYED
/// average, computed as <c>round(clamp(rawVisibleAvg * ConsultantRatingPenaltyFactor, 0, 5), 2)</c>.
/// A penalty event multiplies the persisted factor by one of the constants below;
/// the factor survives future review recomputes, so a deduction lingers
/// proportionally rather than being erased by the next new review.
/// </para>
/// </summary>
public static class ConsultantRatingThresholds
{
    /// <summary>Penalized average below which the consultant is flagged for Admin review.</summary>
    public const decimal LowRatingThreshold = 2.5m;

    /// <summary>
    /// FR-CBR-38: the displayed rating and the low-rating flag are both computed
    /// from the most recent N reviews only ("latest 20 reviews", newest first) —
    /// not the all-time set and not a calendar window. A consultant who recently
    /// improved is not held back by old ratings, and a recent decline is caught
    /// even if the all-time average is still healthy.
    /// </summary>
    public const int RatingWindowSize = 20;

    /// <summary>
    /// Minimum number of visible reviews required before the penalized-average
    /// admin flag fires. Set to 5 to match the booking-intake auto-suspend sample
    /// floor (BookingOptions.LowRatingMinimumSampleSize) — a single noisy 2-star (or
    /// a penalty on a 1-review consultant) should not put a consultant in the admin
    /// low-rated queue.
    /// </summary>
    public const int MinimumReviewsForFlagging = 5;

    // ── Penalty factors (compounding multipliers on the rating average) ──────────

    /// <summary>Consultant cancels a confirmed booking &lt;24h before start: −20% (FR-CBR-16/20).</summary>
    public const decimal CancelLessThan24HoursFactor = 0.80m;

    /// <summary>Admin-validated consultant no-show: −40% (FR-CBR-29).</summary>
    public const decimal ValidatedNoShowFactor = 0.60m;

    /// <summary>Consultant who falsely reported a student no-show: −70% (FR-CBR-32).</summary>
    public const decimal FalseReportFactor = 0.30m;
}
