namespace ScholarPath.Application.Common;

/// <summary>
/// Consultant-booking policy tunables (PB-006), bound from the "Booking" config
/// section. Lives in the Application layer so both the booking command handlers
/// and the Infrastructure jobs can read it.
/// </summary>
public sealed class BookingOptions
{
    public const string SectionName = "Booking";

    /// <summary>Hours a requested booking waits for a consultant response before it auto-expires.</summary>
    public int ConsultantResponseWindowHours { get; set; } = 24;

    /// <summary>
    /// Average rating (FR-094) below which a consultant's booking intake is
    /// auto-suspended pending admin review — evaluated once the rating window is full.
    /// </summary>
    public double LowRatingThreshold { get; set; } = 2.5;

    /// <summary>Number of most-recent visible ratings averaged for the low-rating check.</summary>
    public int LowRatingWindowSize { get; set; } = 20;

    /// <summary>
    /// Minimum number of visible ratings required before the low-rating check is
    /// evaluated at all. FR-CBR-37 averages the *last 20* reviews, i.e. "up to 20":
    /// a consultant with fewer than a full window can still be flagged, but a very
    /// small sample (1-4 reviews) is too noisy to auto-suspend on.
    /// </summary>
    public int LowRatingMinimumSampleSize { get; set; } = 5;

    // ── Student booking-block durations (PB-006R, FR-CBR-15..32) ─────────────────

    /// <summary>Days a student is blocked after cancelling a confirmed booking &lt;24h before start (FR-CBR-18).</summary>
    public int LateCancellationBlockDays { get; set; } = 3;

    /// <summary>Days a student is blocked after an admin-validated no-show (FR-CBR-28).</summary>
    public int ValidatedNoShowBlockDays { get; set; } = 7;

    /// <summary>Days a student is blocked after being falsely reported as a no-show (FR-CBR-31).</summary>
    public int FalseNoShowReportBlockDays { get; set; } = 14;
}
