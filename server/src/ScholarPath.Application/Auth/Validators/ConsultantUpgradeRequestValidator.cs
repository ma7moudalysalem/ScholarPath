using FluentValidation;
using ScholarPath.Application.Auth.DTOs;

namespace ScholarPath.Application.Auth.Validators;

public class ConsultantUpgradeRequestValidator : AbstractValidator<ConsultantUpgradeRequest>
{
    public ConsultantUpgradeRequestValidator()
    {
        RuleFor(x => x.ExperienceSummary)
            .NotEmpty().WithMessage("errors.validation.experienceSummaryRequired")
            .MaximumLength(2000).WithMessage("errors.validation.experienceSummaryMaxLength");

        RuleFor(x => x.ExpertiseTags)
            .NotEmpty().WithMessage("errors.validation.expertiseTagsRequired")
            .MaximumLength(500).WithMessage("errors.validation.expertiseTagsMaxLength");

        RuleFor(x => x.Languages)
            .NotEmpty().WithMessage("errors.validation.languagesRequired")
            .MaximumLength(200).WithMessage("errors.validation.languagesMaxLength");

        RuleFor(x => x.LinkedInUrl)
            .MaximumLength(500).WithMessage("errors.validation.linkedInUrlMaxLength")
            .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("errors.validation.linkedInUrlInvalid")
            .When(x => x.LinkedInUrl is not null);

        RuleFor(x => x.PortfolioUrl)
            .MaximumLength(500).WithMessage("errors.validation.portfolioUrlMaxLength")
            .Must(url => url is null || Uri.TryCreate(url, UriKind.Absolute, out _))
            .WithMessage("errors.validation.portfolioUrlInvalid")
            .When(x => x.PortfolioUrl is not null);
    }
}
