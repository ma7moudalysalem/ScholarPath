using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;

namespace ScholarPath.Application.Applications.Commands.CreateApplication
{
    public class CreateApplicationCommandValidator: AbstractValidator<CreateApplicationCommand>
    {
        public CreateApplicationCommandValidator()
        {
            RuleFor(v => v.ScholarshipId).NotEmpty();
            RuleFor(v => v.PersonalNotes).MaximumLength(4000).WithMessage("الملاحظات يجب ألا تتجاوز 4000 حرف.");
        }
    }
}

