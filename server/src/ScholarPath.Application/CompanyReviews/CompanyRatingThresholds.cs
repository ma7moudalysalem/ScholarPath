namespace ScholarPath.Application.CompanyReviews;

/// <summary>
/// Tunable thresholds for the Company low-rating policy (PB-005R).
///
/// <para>
/// When a Student submits a CompanyReview, the handler recomputes the
/// Company's average rating across its visible reviews (not soft-deleted, not
/// admin-hidden). If the average falls below <see cref="LowRatingThreshold"/>
/// and the Company has at least <see cref="MinimumReviewsForFlagging"/> visible
/// review(s), the Company's <c>UserProfile.CompanyLowRatingFlaggedAt</c> is
/// stamped and the admins are notified — they triage the queue and either
/// clear the flag or suspend the account via the existing
/// <c>SetUserStatusCommand</c>.
/// </para>
///
/// <para>
/// We intentionally do NOT auto-suspend: the consultant-side low-rating policy
/// auto-suspends booking intake only, never the account itself, and the
/// Company side has no equivalent intake to throttle. Suspension of a Company
/// account stays an explicit Admin action.
/// </para>
/// </summary>
public static class CompanyRatingThresholds
{
    /// <summary>Average below which the Company is flagged for Admin review.</summary>
    public const decimal LowRatingThreshold = 2.5m;

    /// <summary>
    /// Minimum number of visible reviews required before the threshold check
    /// fires. Defaults to 1 to match the literal spec ("If Company average
    /// rating is below 2.5: Flag the Company"). Bump this if a "one bad
    /// reviewer can sink a new Company" concern surfaces.
    /// </summary>
    public const int MinimumReviewsForFlagging = 1;
}
