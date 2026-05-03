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

        RuleFor(v => v.TitleEn)
            .MaximumLength(200)
            .NotEmpty().WithMessage("English title is required.");

        RuleFor(v => v.TitleAr)
            .MaximumLength(200)
            .NotEmpty().WithMessage("Arabic title is required.");

        RuleFor(v => v.DescriptionEn)
            .NotEmpty().WithMessage("English description is required.");

        RuleFor(v => v.DescriptionAr)
            .NotEmpty().WithMessage("Arabic description is required.");

        RuleFor(v => v.Deadline)
            .NotEmpty()
            .GreaterThan(DateTime.UtcNow).WithMessage("Deadline must be a future date.");

        RuleFor(v => v.CategoryId)
            .NotEmpty().WithMessage("Scholarship category is required.");
    }
}
