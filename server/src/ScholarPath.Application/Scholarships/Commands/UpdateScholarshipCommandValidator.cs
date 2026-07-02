using System;
using System.Collections.Generic;
using System.Text;

using FluentValidation;

namespace ScholarPath.Application.Scholarships.Commands;

public class UpdateScholarshipCommandValidator : AbstractValidator<UpdateScholarshipCommand>
{
    public UpdateScholarshipCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty();

        // Title caps mirror CreateScholarshipCommandValidator (300) — the two
        // used to disagree (Create=300, Update=200) which let a 250-char title
        // pass create but fail on edit.
        RuleFor(v => v.TitleEn)
            .MaximumLength(300)
            .NotEmpty().WithMessage("English title is required.");

        RuleFor(v => v.TitleAr)
            .MaximumLength(300)
            .NotEmpty().WithMessage("Arabic title is required.");

        RuleFor(v => v.DescriptionEn)
            .NotEmpty().WithMessage("English description is required.")
            .MaximumLength(4000);

        RuleFor(v => v.DescriptionAr)
            .NotEmpty().WithMessage("Arabic description is required.")
            .MaximumLength(4000);

        // Spec: deadline must still be at least 7 days out on update — the
        // previous "just > now" rule let a ScholarshipProvider shorten the deadline to
        // ~tomorrow after publication, bypassing the create-time 7-day rule.
        RuleFor(v => v.Deadline)
            .NotEmpty()
            .Must(deadline => deadline > DateTimeOffset.UtcNow.AddDays(7))
            .WithMessage("Deadline must be at least 7 days from now.");

        RuleFor(v => v.CategoryId)
            .NotEmpty().WithMessage("Scholarship category is required.");
    }
}
