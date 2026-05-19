using ScholarPath.Domain.Common;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A recording of a consultant-booking video session. The bytes live in the
/// configured blob storage; this row holds metadata. Only the booking's
/// student, its consultant, and admins may view or download it.
/// </summary>
public class SessionRecording : AuditableEntity, ISoftDeletable
{
    /// <summary>The booking whose session this recording captured.</summary>
    public Guid BookingId { get; set; }

    /// <summary>The Azure Communication Services recording id.</summary>
    public string RecordingId { get; set; } = default!;

    /// <summary>Provider-prefixed storage key the recording bytes were saved under.</summary>
    public string StoragePath { get; set; } = default!;

    /// <summary>MIME type of the stored recording (typically <c>video/mp4</c>).</summary>
    public string ContentType { get; set; } = "video/mp4";

    /// <summary>Recording file size in bytes.</summary>
    public long SizeBytes { get; set; }

    /// <summary>When the session was recorded.</summary>
    public DateTimeOffset RecordedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ConsultantBooking? Booking { get; set; }
}
