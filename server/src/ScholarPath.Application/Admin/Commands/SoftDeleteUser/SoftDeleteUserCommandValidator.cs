using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.SoftDeleteUser;

public sealed class SoftDeleteUserCommandValidator : AbstractValidator<SoftDeleteUserCommand>
{
    public SoftDeleteUserCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
