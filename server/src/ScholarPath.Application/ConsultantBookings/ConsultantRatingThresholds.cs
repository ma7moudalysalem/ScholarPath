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
    /// Minimum number of visible reviews required before the penalized-average
    /// flag check fires. A penalized average needs at least one review to be
    /// non-null, so 1 matches the literal spec.
    /// </summary>
    public const int MinimumReviewsForFlagging = 1;

    // ── Penalty factors (compounding multipliers on the rating average) ──────────

    /// <summary>Consultant cancels a confirmed booking &lt;24h before start: −20% (FR-CBR-16/20).</summary>
    public const decimal CancelLessThan24HoursFactor = 0.80m;

    /// <summary>Admin-validated consultant no-show: −40% (FR-CBR-29).</summary>
    public const decimal ValidatedNoShowFactor = 0.60m;

    /// <summary>Consultant who falsely reported a student no-show: −70% (FR-CBR-32).</summary>
    public const decimal FalseReportFactor = 0.30m;
}
