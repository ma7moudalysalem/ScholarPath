using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.DTOs;

// ─── Booking read models ──────────────────────────────────────────────────────

/// <summary>
/// One row in a bookings list — the student's "my bookings" page or the
/// consultant's "incoming bookings" page. Flat projection over
/// <see cref="Domain.Entities.ConsultantBooking"/> joined to both participants.
/// </summary>
public sealed record BookingListItemDto
{
    public Guid Id { get; init; }

    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = default!;
    public string? StudentEmail { get; init; }

    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = default!;
    public string? ConsultantPhotoUrl { get; init; }

    /// <summary><see cref="BookingStatus"/> serialised as its string name.</summary>
    public BookingStatus Status { get; init; }

    public DateTimeOffset ScheduledStartAt { get; init; }
    public DateTimeOffset ScheduledEndAt { get; init; }
    public int DurationMinutes { get; init; }
    public decimal PriceUsd { get; init; }
    public string? MeetingUrl { get; init; }

    public DateTimeOffset? RequestedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Full single-booking detail for the student/consultant booking-details pages —
/// every workflow timestamp plus payment / no-show / cancellation fields.
/// </summary>
public sealed record BookingDetailDto
{
    public Guid Id { get; init; }

    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = default!;
    public string? StudentEmail { get; init; }
    public string? StudentPhotoUrl { get; init; }

    public Guid ConsultantId { get; init; }
    public string ConsultantName { get; init; } = default!;
    public string? ConsultantEmail { get; init; }
    public string? ConsultantPhotoUrl { get; init; }

    public Guid? AvailabilityId { get; init; }

    public BookingStatus Status { get; init; }

    public DateTimeOffset ScheduledStartAt { get; init; }
    public DateTimeOffset ScheduledEndAt { get; init; }
    public int DurationMinutes { get; init; }
    public decimal PriceUsd { get; init; }
    public string? MeetingUrl { get; init; }

    // Workflow timestamps
    public DateTimeOffset? RequestedAt { get; init; }
    public DateTimeOffset? ConfirmedAt { get; init; }
    public DateTimeOffset? RejectedAt { get; init; }
    public DateTimeOffset? ExpiredAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public CancellationReason? CancellationReason { get; init; }
    public Guid? CancelledByUserId { get; init; }

    // Payment outcome (FR-092/196) — the linked Payment row's live state.
    public Guid? PaymentId { get; init; }
    public string? StripePaymentIntentId { get; init; }
    /// <summary>Status of the linked Payment row — null when no payment exists.</summary>
    public PaymentStatus? PaymentStatus { get; init; }
    public long? RefundedAmountCents { get; init; }
    public string? RefundReason { get; init; }

    // No-show
    public bool IsNoShowStudent { get; init; }
    public bool IsNoShowConsultant { get; init; }
    public DateTimeOffset? NoShowMarkedAt { get; init; }

    // Meeting attendance (FR-217) — when each party joined the session room.
    public DateTimeOffset? StudentJoinedAt { get; init; }
    public DateTimeOffset? ConsultantJoinedAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

// ─── Availability read models ─────────────────────────────────────────────────

/// <summary>
/// A single saved availability rule — either a weekly-recurring slot
/// (<see cref="DayOfWeek"/> + <see cref="StartTime"/>/<see cref="EndTime"/>) or a
/// one-off ad-hoc slot (<see cref="SpecificStartAt"/>/<see cref="SpecificEndAt"/>).
/// Mirrors <see cref="Domain.Entities.ConsultantAvailability"/>.
/// </summary>
public sealed record AvailabilityRuleDto
{
    public Guid Id { get; init; }
    public Guid ConsultantId { get; init; }

    public bool IsRecurring { get; init; }

    // Weekly-recurring slot
    public DayOfWeek? DayOfWeek { get; init; }
    public TimeOnly? StartTime { get; init; }
    public TimeOnly? EndTime { get; init; }

    // Ad-hoc one-off slot
    public DateTimeOffset? SpecificStartAt { get; init; }
    public DateTimeOffset? SpecificEndAt { get; init; }

    public string Timezone { get; init; } = "UTC";
    public bool IsActive { get; init; }
}

/// <summary>
/// A concrete, bookable time window for a consultant — the result of expanding
/// recurring + ad-hoc <see cref="Domain.Entities.ConsultantAvailability"/> rules
/// into dated slots and removing windows already taken by a non-cancelled booking.
/// </summary>
public sealed record BookableSlotDto
{
    /// <summary>The availability rule this slot was expanded from.</summary>
    public Guid AvailabilityId { get; init; }

    public DateTimeOffset StartAt { get; init; }
    public DateTimeOffset EndAt { get; init; }
    public int DurationMinutes { get; init; }

    public bool IsRecurring { get; init; }
    public string Timezone { get; init; } = "UTC";
}
