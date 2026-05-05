using FluentValidation;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Validators;

public class UpdateRemindersRequestValidator : AbstractValidator<UpdateRemindersRequest>
{
    private static readonly int[] AllowedPresets = [1, 3, 7, 14, 30];

    public UpdateRemindersRequestValidator()
    {
        RuleFor(x => x.Presets)
            .Must(p => p.Length <= 5)
            .WithMessage("errors.validation.reminderPresetsMaxFive");

        RuleForEach(x => x.Presets)
            .Must(p => AllowedPresets.Contains(p))
            .WithMessage("errors.validation.invalidReminderPreset");
    }
}
