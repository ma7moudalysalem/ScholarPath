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
/// Result of <see cref="RequestBookingCommand"/>.
/// <para>
/// For paid bookings (session fee &gt; 0): <see cref="ClientSecret"/> is the
/// Stripe client secret of the booking's manual-capture PaymentIntent — the
/// checkout widget confirms THIS intent, so a booking has exactly one intent
/// rather than a duplicate created at checkout (PB-006 gap report, Problem 1).
/// </para>
/// <para>
/// For free bookings (session fee = 0): no PaymentIntent is created, the
/// Stripe fields are <c>null</c>, and <see cref="IsFree"/> is <c>true</c> so
/// the client can skip the checkout widget entirely.
/// </para>
/// </summary>
public sealed record RequestBookingResult(
    Guid BookingId,
    bool IsFree,
    string? ClientSecret,
    string? PaymentIntentId);
