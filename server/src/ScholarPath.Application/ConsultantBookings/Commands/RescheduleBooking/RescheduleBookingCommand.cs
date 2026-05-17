using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;

/// <summary>
/// FR-229 — reschedule an accepted (or still-requested) consultant booking to a
/// new time slot without taking a new payment. The existing payment hold /
/// captured charge carries over unchanged; only the scheduled times move.
/// </summary>
[Auditable(
    AuditAction.Update,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Booking rescheduled: {TargetId}"
)]
public sealed record RescheduleBookingCommand(
    Guid BookingId,
    Guid? AvailabilityId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt
) : IRequest;
