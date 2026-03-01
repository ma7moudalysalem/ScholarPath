using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class LinkProviderRequestValidator : AbstractValidator<LinkProviderRequest>
{
    public LinkProviderRequestValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty().WithMessage("errors.auth.providerRequired");

        RuleFor(x => x.ProviderToken)
            .NotEmpty().WithMessage("errors.auth.providerTokenRequired");
    }
}
