using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;

[Auditable(
    AuditAction.BookingRequested,
    "ConsultantBooking",
    SummaryTemplate = "Booking requested: {TargetId}"
)]
public sealed record RequestBookingCommand(
    Guid ConsultantId,
    Guid? AvailabilityId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    string Timezone,
    string? Notes
) : IRequest<Guid>;
