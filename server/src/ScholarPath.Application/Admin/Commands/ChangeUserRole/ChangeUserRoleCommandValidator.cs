using FluentValidation;

namespace ScholarPath.Application.Admin.Commands.ChangeUserRole;

public sealed class ChangeUserRoleCommandValidator : AbstractValidator<ChangeUserRoleCommand>
{
    private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "ScholarshipProvider", "Consultant", "Admin", "SuperAdmin", "Moderator",
    };

    public ChangeUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Operation).IsInEnum();
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be one of: Student, ScholarshipProvider, Consultant, Admin, SuperAdmin, Moderator.");
    }
}
