using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("errors.validation.emailRequired")
            .MaximumLength(256).WithMessage("errors.validation.emailMaxLength")
            .EmailAddress().WithMessage("errors.validation.emailInvalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("errors.validation.passwordRequired")
            .MaximumLength(256).WithMessage("errors.validation.maxLength");


    }
}
