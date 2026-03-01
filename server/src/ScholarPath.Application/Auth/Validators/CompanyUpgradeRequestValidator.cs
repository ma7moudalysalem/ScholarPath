using System.Text.RegularExpressions;
using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class CompanyUpgradeRequestValidator : AbstractValidator<CompanyUpgradeRequest>
{
    // CRN: 4-30 chars, letters + numbers + hyphen only
    private static readonly Regex CrnRegex = new(@"^[a-zA-Z0-9\-]{4,30}$", RegexOptions.Compiled);

    public CompanyUpgradeRequestValidator()
    {
        RuleFor(x => x.CompanyName)
            .NotEmpty().WithMessage("errors.validation.companyNameRequired")
            .MinimumLength(2).WithMessage("errors.validation.companyNameMinLength")
            .MaximumLength(200).WithMessage("errors.validation.companyNameMaxLength");

        RuleFor(x => x.CompanyCountry)
            .NotEmpty().WithMessage("errors.validation.companyCountryRequired")
            .MaximumLength(100).WithMessage("errors.validation.companyCountryMaxLength");

        RuleFor(x => x.CompanyWebsite)
            .MaximumLength(500).WithMessage("errors.validation.companyWebsiteMaxLength")
            .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("errors.validation.companyWebsiteInvalid")
            .When(x => x.CompanyWebsite is not null);

        RuleFor(x => x.ContactPersonName)
            .NotEmpty().WithMessage("errors.validation.contactPersonNameRequired")
            .MaximumLength(120).WithMessage("errors.validation.contactPersonNameMaxLength");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("errors.validation.contactEmailRequired")
            .MaximumLength(150).WithMessage("errors.validation.contactEmailMaxLength")
            .EmailAddress().WithMessage("errors.validation.contactEmailInvalid");

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20).WithMessage("errors.validation.contactPhoneMaxLength")
            .When(x => x.ContactPhone is not null);

        // CRN: 4-30 chars, letters + numbers + hyphen only
        RuleFor(x => x.CompanyRegistrationNumber)
            .NotEmpty().WithMessage("errors.validation.crnRequired")
            .Must(crn => crn is not null && CrnRegex.IsMatch(crn))
            .WithMessage("errors.validation.crnInvalid");
    }
}
