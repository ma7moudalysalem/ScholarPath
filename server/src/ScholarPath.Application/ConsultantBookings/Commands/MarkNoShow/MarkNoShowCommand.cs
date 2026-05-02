using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;

[Auditable(
    AuditAction.BookingNoShowMarked,
    "ConsultantBooking",
    TargetIdProperty = nameof(BookingId),
    SummaryTemplate = "Booking no-show marked: {TargetId}"
)]
public sealed record MarkNoShowCommand(
    Guid BookingId
) : IRequest;
