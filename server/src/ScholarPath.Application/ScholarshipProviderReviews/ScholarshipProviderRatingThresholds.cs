namespace ScholarPath.Application.ScholarshipProviderReviews;

/// <summary>
/// Tunable thresholds for the ScholarshipProvider low-rating policy (PB-005R).
///
/// <para>
/// When a Student submits a ScholarshipProviderReview, the handler recomputes the
/// ScholarshipProvider's average rating across its visible reviews (not soft-deleted, not
/// admin-hidden). If the average falls below <see cref="LowRatingThreshold"/>
/// and the ScholarshipProvider has at least <see cref="MinimumReviewsForFlagging"/> visible
/// review(s), the ScholarshipProvider's <c>UserProfile.ScholarshipProviderLowRatingFlaggedAt</c> is
/// stamped and the admins are notified — they triage the queue and either
/// clear the flag or suspend the account via the existing
/// <c>SetUserStatusCommand</c>.
/// </para>
///
/// <para>
/// We intentionally do NOT auto-suspend: the consultant-side low-rating policy
/// auto-suspends booking intake only, never the account itself, and the
/// ScholarshipProvider side has no equivalent intake to throttle. Suspension of a ScholarshipProvider
/// account stays an explicit Admin action.
/// </para>
/// </summary>
public static class ScholarshipProviderRatingThresholds
{
    /// <summary>Average below which the ScholarshipProvider is flagged for Admin review.</summary>
    public const decimal LowRatingThreshold = 2.5m;

    /// <summary>
    /// Minimum number of visible reviews (within the flagging window) required
    /// before the threshold check fires. Defaults to 1 to match the literal
    /// spec ("If ScholarshipProvider average rating is below 2.5: Flag the
    /// ScholarshipProvider"). Bump this if a "one bad reviewer can sink a new
    /// ScholarshipProvider" concern surfaces.
    /// </summary>
    public const int MinimumReviewsForFlagging = 1;

    /// <summary>
    /// FR-APP-35: the flag decision considers only reviews created within this
    /// trailing window (in months). The displayed all-time average is unaffected
    /// — only the flag trigger is time-scoped.
    /// </summary>
    public const int FlaggingWindowMonths = 3;
}
