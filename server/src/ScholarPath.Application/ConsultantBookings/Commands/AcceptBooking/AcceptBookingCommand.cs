using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;

public sealed record AcceptBookingCommand(
    Guid BookingId,
    string MeetingUrl
) : IRequest;
