using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.ReviewUpgradeRequest;

public sealed class ReviewUpgradeRequestCommandValidator : AbstractValidator<ReviewUpgradeRequestCommand>
{
    public ReviewUpgradeRequestCommandValidator()
    {
        RuleFor(x => x.RequestId).NotEmpty();
        RuleFor(x => x.Decision).IsInEnum();
        RuleFor(x => x.ReviewerNotes).MaximumLength(1000);

        RuleFor(x => x.ReviewerNotes)
            .NotEmpty()
            .When(x => x.Decision == UpgradeDecision.Reject)
            .WithMessage("Reviewer notes are required when rejecting an upgrade request.");
    }
}
