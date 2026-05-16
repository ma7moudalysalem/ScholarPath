using FluentValidation;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.ReviewApplication;

public sealed class ReviewApplicationCommandValidator : AbstractValidator<ReviewApplicationCommand>
{
    public ReviewApplicationCommandValidator()
    {
        RuleFor(v => v.ApplicationId).NotEmpty();
        RuleFor(v => v.Status).Must(s => s is ApplicationStatus.Accepted or ApplicationStatus.Rejected)
            .WithMessage("Review status must be Accepted or Rejected.");
    }
}
