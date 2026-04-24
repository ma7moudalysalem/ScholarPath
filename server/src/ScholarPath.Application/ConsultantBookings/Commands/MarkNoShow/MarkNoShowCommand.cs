using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;

public sealed record MarkNoShowCommand(
    Guid BookingId
) : IRequest;
