using MediatR;

namespace ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

public sealed record UpdateAvailabilityCommand(
    bool ReplaceExisting,
    List<AvailabilityInputModel> Slots
) : IRequest<Unit>;

public sealed record AvailabilityInputModel(
    bool IsRecurring,
    DayOfWeek? DayOfWeek,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    DateTimeOffset? SpecificStartAt,
    DateTimeOffset? SpecificEndAt,
    string Timezone,
    bool IsActive
);
