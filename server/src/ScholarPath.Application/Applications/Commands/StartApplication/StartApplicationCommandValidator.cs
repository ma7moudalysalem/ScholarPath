using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

namespace ScholarPath.Application.Applications.Commands.StartApplication;

public sealed class StartApplicationCommandValidator : AbstractValidator<StartApplicationCommand>
{
    public StartApplicationCommandValidator()
    {
        RuleFor(v => v.ScholarshipId)
            .NotEmpty()
            .WithMessage("Scholarship identifier is required.");

        RuleFor(v => v.PersonalNotes)
            .MaximumLength(4000)
            .WithMessage("Notes must not exceed 4000 characters.");
    }
}
