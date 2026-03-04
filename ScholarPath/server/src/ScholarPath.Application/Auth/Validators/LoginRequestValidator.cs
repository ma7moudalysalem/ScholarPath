using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Identifier)
            .NotEmpty().WithMessage("errors.validation.identifierRequired")
            .MaximumLength(256).WithMessage("errors.validation.identifierMaxLength");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("errors.validation.passwordRequired")
            .MaximumLength(256).WithMessage("errors.validation.maxLength");
    }
}
