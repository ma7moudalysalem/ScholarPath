using FluentValidation;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Validators;

public class CompanyUpgradeRequestValidator : AbstractValidator<CompanyUpgradeRequest>
{
    public CompanyUpgradeRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("errors.validation.companyNameRequired")
            .MaximumLength(200).WithMessage("errors.validation.companyNameMaxLength");

        RuleFor(x => x.Country)
            .NotEmpty().WithMessage("errors.validation.countryRequired")
            .MaximumLength(100).WithMessage("errors.validation.countryMaxLength");

        RuleFor(x => x.ContactPersonName)
            .NotEmpty().WithMessage("errors.validation.contactPersonNameRequired")
            .MaximumLength(120).WithMessage("errors.validation.contactPersonNameMaxLength");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("errors.validation.contactEmailRequired")
            .EmailAddress().WithMessage("errors.validation.contactEmailInvalid")
            .MaximumLength(150).WithMessage("errors.validation.contactEmailMaxLength");

        When(x => !string.IsNullOrWhiteSpace(x.ContactPhone), () =>
        {
            RuleFor(x => x.ContactPhone!)
                .MaximumLength(20).WithMessage("errors.validation.contactPhoneMaxLength");
        });

        RuleFor(x => x.CompanyRegistrationNumber)
            .NotEmpty().WithMessage("errors.validation.crnRequired")
            .MinimumLength(4).WithMessage("errors.validation.crnMinLength")
            .MaximumLength(30).WithMessage("errors.validation.crnMaxLength")
            .Matches(@"^[a-zA-Z0-9\-]+$").WithMessage("errors.validation.crnInvalidFormat");

        When(x => !string.IsNullOrWhiteSpace(x.Website), () =>
        {
            RuleFor(x => x.Website!)
                .MaximumLength(500).WithMessage("errors.validation.websiteMaxLength");
        });
    }
}
