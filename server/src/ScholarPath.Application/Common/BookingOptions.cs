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
}
