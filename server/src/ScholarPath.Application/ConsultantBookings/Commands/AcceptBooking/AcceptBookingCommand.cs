using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;

[Auditable(
    AuditAction.BookingAccepted,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Booking accepted: {TargetId}"
)]
public sealed record AcceptBookingCommand(
    Guid BookingId,
    string MeetingUrl
) : IRequest;
