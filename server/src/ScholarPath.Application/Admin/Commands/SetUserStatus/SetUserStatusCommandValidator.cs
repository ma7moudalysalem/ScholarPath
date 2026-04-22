using FluentValidation;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Admin.Commands.SetUserStatus;

public sealed class SetUserStatusCommandValidator : AbstractValidator<SetUserStatusCommand>
{
    public SetUserStatusCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.NewStatus)
            .IsInEnum()
            .NotEqual(AccountStatus.Unassigned)
            .WithMessage("Cannot move a user back to Unassigned via admin action.");

        RuleFor(x => x.Reason)
            .NotEmpty()
            .When(x => x.NewStatus is AccountStatus.Suspended or AccountStatus.Deactivated)
            .WithMessage("A reason is required when suspending or deactivating a user.");

        RuleFor(x => x.Reason)
            .MaximumLength(500);
    }
}
