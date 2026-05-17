using FluentValidation;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    public SelectRoleCommandValidator() =>
        RuleFor(x => x.Role)
            .Must(r => r is "Student" or "Company" or "Consultant")
            .WithMessage("Role must be Student, Company, or Consultant.");
}
