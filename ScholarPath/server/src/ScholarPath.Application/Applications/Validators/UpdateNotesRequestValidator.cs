using FluentValidation;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Validators;

public class UpdateNotesRequestValidator : AbstractValidator<UpdateNotesRequest>
{
    public UpdateNotesRequestValidator()
    {
        RuleFor(x => x.Notes)
            .MaximumLength(2000).WithMessage("errors.validation.notesTooLong");
    }
}
