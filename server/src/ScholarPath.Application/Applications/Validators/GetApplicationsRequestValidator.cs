using FluentValidation;
using ScholarPath.Application.Applications.DTOs;

namespace ScholarPath.Application.Applications.Validators;

public class GetApplicationsRequestValidator : AbstractValidator<GetApplicationsRequest>
{
    private static readonly string[] ValidSortValues = ["deadline", "updatedat"];

    public GetApplicationsRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("errors.validation.pageMinValue");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("errors.validation.pageSizeRange");

        RuleFor(x => x.SortBy)
            .Must(v => ValidSortValues.Contains(v.ToLowerInvariant()))
            .When(x => !string.IsNullOrWhiteSpace(x.SortBy))
            .WithMessage("errors.validation.invalidSortBy");
    }
}
