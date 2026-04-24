using FluentValidation;

namespace ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;

public sealed class ConfigureReviewFeeCommandValidator : AbstractValidator<ConfigureReviewFeeCommand>
{
    public ConfigureReviewFeeCommandValidator()
    {
        RuleFor(v => v.ScholarshipId)
            .NotEmpty().WithMessage("ScholarshipId is required.");

        RuleFor(v => v.ReviewFeeUsd)
            .GreaterThanOrEqualTo(0).WithMessage("Review fee must be 0 or greater.");
    }
}
