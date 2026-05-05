using FluentValidation;
using ScholarPath.Application.UpgradeRequests.DTOs;

namespace ScholarPath.Application.UpgradeRequests.Validators;

public class ConsultantUpgradeRequestValidator : AbstractValidator<ConsultantUpgradeRequest>
{
    public ConsultantUpgradeRequestValidator()
    {
        RuleFor(x => x.Education)
            .NotEmpty().WithMessage("errors.validation.educationRequired");

        RuleForEach(x => x.Education).ChildRules(education =>
        {
            education.RuleFor(e => e.InstitutionName)
                .NotEmpty().WithMessage("errors.validation.institutionNameRequired")
                .MaximumLength(300).WithMessage("errors.validation.institutionNameMaxLength");

            education.RuleFor(e => e.DegreeName)
                .NotEmpty().WithMessage("errors.validation.degreeNameRequired")
                .MaximumLength(200).WithMessage("errors.validation.degreeNameMaxLength");

            education.RuleFor(e => e.FieldOfStudy)
                .NotEmpty().WithMessage("errors.validation.fieldOfStudyRequired")
                .MaximumLength(200).WithMessage("errors.validation.fieldOfStudyMaxLength");

            education.RuleFor(e => e.StartYear)
                .InclusiveBetween(1950, DateTime.UtcNow.Year + 5)
                .WithMessage("errors.validation.startYearInvalid");
        });

        RuleFor(x => x.ExperienceSummary)
            .NotEmpty().WithMessage("errors.validation.experienceSummaryRequired")
            .MinimumLength(50).WithMessage("errors.validation.experienceSummaryMinLength")
            .MaximumLength(1500).WithMessage("errors.validation.experienceSummaryMaxLength");

        RuleFor(x => x.ExpertiseTags)
            .NotEmpty().WithMessage("errors.validation.expertiseTagsRequired")
            .Must(tags => tags.Count <= 10).WithMessage("errors.validation.expertiseTagsMaxCount");

        RuleFor(x => x.Languages)
            .NotEmpty().WithMessage("errors.validation.languagesRequired")
            .Must(langs => langs.Count <= 5).WithMessage("errors.validation.languagesMaxCount");

        When(x => x.Links is not null, () =>
        {
            RuleFor(x => x.Links!)
                .Must(links => links.Count <= 3).WithMessage("errors.validation.linksMaxCount");

            RuleForEach(x => x.Links!).ChildRules(link =>
            {
                link.RuleFor(l => l.Url)
                    .NotEmpty().WithMessage("errors.validation.linkUrlRequired")
                    .MaximumLength(500).WithMessage("errors.validation.linkUrlMaxLength");

                link.RuleFor(l => l.Label)
                    .NotEmpty().WithMessage("errors.validation.linkLabelRequired");
            });
        });
    }
}
