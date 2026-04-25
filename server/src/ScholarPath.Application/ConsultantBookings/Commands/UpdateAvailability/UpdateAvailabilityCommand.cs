using MediatR;
using ScholarPath.Application.Common.Auditing;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

[Auditable(
    AuditAction.ConsultantAvailabilityUpdated,
    "ConsultantAvailability",
    SummaryTemplate = "Consultant availability updated"
)]
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
