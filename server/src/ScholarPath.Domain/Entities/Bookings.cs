using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

public class ConsultantAvailability : AuditableEntity, ISoftDeletable
{
    public Guid ConsultantId { get; set; }

    // Weekly recurring slot (applies every week on this day/time)
    public DayOfWeek? DayOfWeek { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Ad-hoc specific slot (one-off)
    public DateTimeOffset? SpecificStartAt { get; set; }
    public DateTimeOffset? SpecificEndAt { get; set; }

    public string Timezone { get; set; } = "UTC";
    public bool IsRecurring { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Consultant { get; set; }
}

public class ConsultantBooking : AuditableEntity, ISoftDeletable
{
    public Guid StudentId { get; set; }
    public Guid ConsultantId { get; set; }
    public Guid? AvailabilityId { get; set; }

    public DateTimeOffset ScheduledStartAt { get; set; }
    public DateTimeOffset ScheduledEndAt { get; set; }
    public int DurationMinutes { get; set; }
    public decimal PriceUsd { get; set; }
    public string? MeetingUrl { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Requested;
    public DateTimeOffset? RequestedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? ExpiredAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public CancellationReason? CancellationReason { get; set; }
    public Guid? CancelledByUserId { get; set; }

    // Payment
    public Guid? PaymentId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    // No-show
    public bool IsNoShowStudent { get; set; }
    public bool IsNoShowConsultant { get; set; }
    public DateTimeOffset? NoShowMarkedAt { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedByUserId { get; set; }

    public ApplicationUser? Student { get; set; }
    public ApplicationUser? Consultant { get; set; }
    public ConsultantAvailability? Availability { get; set; }
    public Payment? Payment { get; set; }
   
}
