using FluentValidation;

namespace ScholarPath.Application.ScholarshipProviderReviews.Commands.SubmitScholarshipProviderRating;

public sealed class SubmitScholarshipProviderRatingCommandValidator : AbstractValidator<SubmitScholarshipProviderRatingCommand>
{
    public SubmitScholarshipProviderRatingCommandValidator()
    {
        RuleFor(v => v.ApplicationId).NotEmpty();
        RuleFor(v => v.Rating).InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");
        RuleFor(v => v.Comment).MaximumLength(1000).WithMessage("Comment cannot exceed 1000 characters.");
    }
}
