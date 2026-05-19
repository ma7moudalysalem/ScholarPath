using FluentValidation;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    public SelectRoleCommandValidator()
    {
        RuleFor(x => x.Role)
            .Must(r => r is "Student" or "Company" or "Consultant")
            .WithMessage("Role must be Student, Company, or Consultant.");

        // Company / Consultant must supply their onboarding details.
        RuleFor(x => x.Details)
            .NotNull()
            .When(x => x.Role is "Company" or "Consultant")
            .WithMessage("Onboarding details are required for this role.");

        When(x => x.Details is not null, () =>
        {
            RuleFor(x => x.Details!.OrganizationLegalName)
                .NotEmpty().MaximumLength(200)
                .When(x => x.Role == "Company")
                .WithMessage("Organization legal name is required.");

            RuleFor(x => x.Details!.OrganizationWebsite)
                .MaximumLength(300);

            RuleFor(x => x.Details!.Biography)
                .NotEmpty().MaximumLength(2000)
                .When(x => x.Role == "Consultant")
                .WithMessage("A short bio is required.");

            RuleFor(x => x.Details!.SessionFeeUsd)
                .NotNull().GreaterThan(0)
                .When(x => x.Role == "Consultant")
                .WithMessage("Session fee must be greater than zero.");
        });
    }
}
