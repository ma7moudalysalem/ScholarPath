using FluentValidation;

namespace ScholarPath.Application.Auth.Commands.SwitchRole;

public sealed class SwitchRoleCommandValidator : AbstractValidator<SwitchRoleCommand>
{
    public SwitchRoleCommandValidator() => RuleFor(x => x.TargetRole).NotEmpty();
}
