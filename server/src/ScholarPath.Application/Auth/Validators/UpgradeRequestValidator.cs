using FluentValidation;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

public class UpgradeRequestValidator : AbstractValidator<UpgradeRequest>
{
    public UpgradeRequestValidator()
    {
        RuleFor(x => x.ExperienceSummary)
            .NotEmpty()
            .MaximumLength(1500)
            .WithMessage("Experience summary must be under 1500 characters.");

        RuleFor(x => x.Languages)
            .Must(l => l != null && l.Count <= 5)
            .WithMessage("You can add up to 5 languages only.");

        // valedat -- list of Education
        RuleForEach(x => x.EducationEntries).ChildRules(edu => {
            edu.RuleFor(e => e.InstitutionName).NotEmpty();
            edu.RuleFor(e => e.DegreeName).NotEmpty();
            edu.RuleFor(e => e.StartYear).LessThanOrEqualTo(DateTime.Now.Year);
        });

        // validate links
        RuleForEach(x => x.upgradeRequestLinks).ChildRules(link => {
            link.RuleFor(l => l.Url).NotEmpty().Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
        });
    }
}
