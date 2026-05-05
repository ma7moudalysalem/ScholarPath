using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("errors.validation.refreshTokenRequired")
            .MaximumLength(2048).WithMessage("errors.validation.maxLength");
    }
}
