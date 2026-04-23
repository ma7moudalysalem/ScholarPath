using FluentValidation;

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
