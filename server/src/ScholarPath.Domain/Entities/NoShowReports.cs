using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A no-show report awaiting admin validation (PB-006R, FR-CBR-25..32). Either
/// party can report the other as a no-show; NO penalty, refund, or block applies
/// until an admin validates the report. Holding the whole validation state on its
/// own entity (rather than overloading <see cref="ConsultantBooking.Status"/>) lets
/// us record who reported whom, the admin's verdict, and support both-party reports.
/// </summary>
public class NoShowReport : AuditableEntity, ISoftDeletable
{
    public Guid BookingId { get; set; }

    /// <summary>The user who filed the report (student or consultant).</summary>
    public Guid ReporterUserId { get; set; }

    /// <summary>The user accused of not showing up.</summary>
    public Guid AccusedUserId { get; set; }

    /// <summary>Which role the accused party held in the booking.</summary>
    public NoShowAccusedRole AccusedRole { get; set; }

    public NoShowReportStatus Status { get; set; } = NoShowReportStatus.PendingReview;

    public Guid? ResolvedByAdminId { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }

    /// <summary>Admin's note recorded at resolution time.</summary>
    public string? AdminNote { get; set; }

    /// <summary>Optional note supplied by the reporter.</summary>
    public string? ReporterNote { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ConsultantBooking? Booking { get; set; }
    public ApplicationUser? Reporter { get; set; }
    public ApplicationUser? Accused { get; set; }
}
