using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;

public sealed record RejectBookingCommand(
    Guid BookingId
) : IRequest;
