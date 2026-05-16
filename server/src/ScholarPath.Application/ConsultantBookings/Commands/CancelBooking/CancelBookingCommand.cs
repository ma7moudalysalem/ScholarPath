using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;

[Auditable(
    AuditAction.BookingCancelled,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Booking cancelled: {TargetId}"
)]
public sealed record CancelBookingCommand(
    Guid BookingId
) : IRequest;
