using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;

[Auditable(
    AuditAction.BookingRequested,
    "ConsultantBooking",
    TargetIdProperty = nameof(RequestBookingResult.BookingId),
    SummaryTemplate = "Booking requested: {TargetId}"
)]
public sealed record RequestBookingCommand(
    Guid ConsultantId,
    Guid? AvailabilityId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    string Timezone,
    string? Notes
) : IRequest<RequestBookingResult>;

/// <summary>
/// Result of <see cref="RequestBookingCommand"/>. <see cref="ClientSecret"/> is
/// the Stripe client secret of the booking's manual-capture PaymentIntent — the
/// checkout widget confirms THIS intent, so a booking has exactly one intent
/// rather than a duplicate created at checkout (PB-006 gap report, Problem 1).
/// </summary>
public sealed record RequestBookingResult(
    Guid BookingId,
    string? ClientSecret,
    string? PaymentIntentId);
