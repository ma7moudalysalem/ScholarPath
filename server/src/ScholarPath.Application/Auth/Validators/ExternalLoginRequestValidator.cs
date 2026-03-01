using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class ExternalLoginRequestValidator : AbstractValidator<ExternalLoginRequest>
{
    public ExternalLoginRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("errors.validation.providerRequired")
            .MaximumLength(50).WithMessage("errors.validation.maxLength");

        RuleFor(x => x.ProviderToken)
            .NotEmpty().WithMessage("errors.validation.tokenRequired")
            .MaximumLength(4096).WithMessage("errors.validation.maxLength");
    }
}
