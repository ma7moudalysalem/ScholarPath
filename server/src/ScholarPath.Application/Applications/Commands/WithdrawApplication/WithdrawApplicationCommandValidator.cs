using FluentValidation;

namespace ScholarPath.Application.Applications.Commands.WithdrawApplication;

public sealed class WithdrawApplicationCommandValidator : AbstractValidator<WithdrawApplicationCommand>
{
    public WithdrawApplicationCommandValidator()
    {
        RuleFor(v => v.ApplicationId).NotEmpty();
    }
}
