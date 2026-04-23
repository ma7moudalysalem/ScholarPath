using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;

public sealed record RequestBookingCommand(
    Guid ConsultantId,
    Guid? AvailabilityId,
    DateTimeOffset ScheduledStartAt,
    DateTimeOffset ScheduledEndAt,
    string Timezone,
    string? Notes
) : IRequest<Guid>;
