using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;

public sealed record CancelBookingCommand(
    Guid BookingId
) : IRequest;
