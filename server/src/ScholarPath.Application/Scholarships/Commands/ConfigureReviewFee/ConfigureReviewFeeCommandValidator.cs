using FluentValidation;

namespace ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;

public sealed class ConfigureReviewFeeCommandValidator : AbstractValidator<ConfigureReviewFeeCommand>
{
    public ConfigureReviewFeeCommandValidator()
    {
        RuleFor(v => v.ScholarshipId)
            .NotEmpty().WithMessage("ScholarshipId is required.");

        RuleFor(v => v.ReviewFeeUsd)
           .GreaterThanOrEqualTo(0m)
           .LessThanOrEqualTo(500m)
            .WithMessage("Review fee must be between $0 and $500.");
        }
}
