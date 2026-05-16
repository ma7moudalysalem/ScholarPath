using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;

[Auditable(
    AuditAction.BookingRejected,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Booking rejected: {TargetId}"
)]
public sealed record RejectBookingCommand(
    Guid BookingId
) : IRequest;
