using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("errors.validation.emailRequired")
            .EmailAddress().WithMessage("errors.validation.emailInvalid")
            .MaximumLength(256).WithMessage("errors.validation.emailMaxLength");
    }
}
