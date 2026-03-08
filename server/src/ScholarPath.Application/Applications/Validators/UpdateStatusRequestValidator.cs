using FluentValidation;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Validators;

public class UpdateStatusRequestValidator : AbstractValidator<UpdateStatusRequest>
{
    public UpdateStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("errors.validation.invalidStatus");
    }
}
