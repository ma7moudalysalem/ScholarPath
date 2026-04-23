using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.ApproveOnboarding;

public sealed class ReviewOnboardingCommandValidator : AbstractValidator<ReviewOnboardingCommand>
{
    public ReviewOnboardingCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.ReviewerNotes).MaximumLength(1000);

        RuleFor(x => x.ReviewerNotes)
            .NotEmpty()
            .When(x => x.Decision == OnboardingDecision.Reject)
            .WithMessage("Reviewer notes are required when rejecting an onboarding request.");
    }
}
