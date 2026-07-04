using FluentValidation;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.ReviewApplication;

public sealed class ReviewApplicationCommandValidator : AbstractValidator<ReviewApplicationCommand>
{
    public ReviewApplicationCommandValidator()
    {
        RuleFor(v => v.ApplicationId).NotEmpty();
        // BUG-02: the allowed decision set must stay in sync with
        // ApplicationStateMachine (the authoritative from->to guard in the handler,
        // which still rejects illegal transitions with a 409). Shortlisted and
        // UnderReview are legitimate provider review targets; omitting Shortlisted
        // wrongly 422'd a valid shortlist decision.
        RuleFor(v => v.Status)
            .Must(s => s is ApplicationStatus.UnderReview
                        or ApplicationStatus.Shortlisted
                        or ApplicationStatus.Accepted
                        or ApplicationStatus.Rejected)
            .WithMessage("Review status must be UnderReview, Shortlisted, Accepted, or Rejected.");

        // FR-APP-30: rejecting an application REQUIRES a rejection reason. The
        // reason is stored and shown to the Student, so a blank reject must be
        // rejected at the API boundary (the client also disables the Reject
        // button until the field has text).
        RuleFor(v => v.DecisionReason)
            .NotEmpty()
            .WithMessage("A rejection reason is required when rejecting an application.")
            .MaximumLength(1000)
            .When(v => v.Status == ApplicationStatus.Rejected);
    }
}
