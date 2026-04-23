using FluentValidation;
using System.Linq;

namespace ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

public sealed class UpdateAvailabilityCommandValidator : AbstractValidator<UpdateAvailabilityCommand>
{
    public UpdateAvailabilityCommandValidator()
    {
        RuleFor(x => x.Slots)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("At least one availability slot is required.");

        RuleForEach(x => x.Slots)
            .SetValidator(new AvailabilityInputModelValidator());

        RuleFor(x => x.Slots)
            .Must(NotHaveOverlappingRecurringSlots)
            .WithMessage("Recurring availability slots must not overlap.");
    }

    private static bool NotHaveOverlappingRecurringSlots(List<AvailabilityInputModel> slots)
    {
        var recurringSlots = slots
            .Where(x => x.IsRecurring && x.DayOfWeek.HasValue && x.StartTime.HasValue && x.EndTime.HasValue)
            .GroupBy(x => x.DayOfWeek!.Value);

        foreach (var dayGroup in recurringSlots)
        {
            var ordered = dayGroup
                .OrderBy(x => x.StartTime!.Value)
                .ToList();

            for (var i = 0; i < ordered.Count - 1; i++)
            {
                var current = ordered[i];
                var next = ordered[i + 1];

                if (current.EndTime!.Value > next.StartTime!.Value)
                {
                    return false;
                }
            }
        }

        return true;
    }
}

public sealed class AvailabilityInputModelValidator : AbstractValidator<AvailabilityInputModel>
{
    public AvailabilityInputModelValidator()
    {
        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64);

        When(x => x.IsRecurring, () =>
        {
            RuleFor(x => x.DayOfWeek).NotNull();
            RuleFor(x => x.StartTime).NotNull();
            RuleFor(x => x.EndTime).NotNull();

            RuleFor(x => x)
                .Must(x => x.StartTime < x.EndTime)
                .WithMessage("StartTime must be earlier than EndTime for recurring slots.");
        });

        When(x => !x.IsRecurring, () =>
        {
            RuleFor(x => x.SpecificStartAt).NotNull();
            RuleFor(x => x.SpecificEndAt).NotNull();

            RuleFor(x => x)
                .Must(x => x.SpecificStartAt < x.SpecificEndAt)
                .WithMessage("SpecificStartAt must be earlier than SpecificEndAt for ad-hoc slots.");
        });
    }
}
