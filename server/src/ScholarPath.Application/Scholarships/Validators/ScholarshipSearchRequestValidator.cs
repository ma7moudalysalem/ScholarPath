using FluentValidation;
using ScholarPath.Application.Scholarships.DTOs;

namespace ScholarPath.Application.Scholarships.Validators;

public class ScholarshipSearchRequestValidator : AbstractValidator<ScholarshipSearchRequest>
{
    public ScholarshipSearchRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("errors.validation.pageMinValue");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("errors.validation.pageSizeRange");

        RuleFor(x => x.DeadlineTo)
            .GreaterThanOrEqualTo(x => x.DeadlineFrom)
            .When(x => x.DeadlineFrom.HasValue && x.DeadlineTo.HasValue)
            .WithMessage("errors.validation.deadlineRangeInvalid");

        RuleFor(x => x.SortBy)
            .IsInEnum().WithMessage("errors.validation.invalidSortBy");
    }
}
