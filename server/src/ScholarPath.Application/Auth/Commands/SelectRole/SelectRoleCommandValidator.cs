using FluentValidation;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    // Allowed Company Type values — kept as a string set so we don't ripple a new enum
    // through migrations and the snapshot. The frontend mirrors this list.
    private static readonly string[] AllowedCompanyTypes =
        ["University", "NGO", "Company", "Foundation", "Government", "Other"];

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

        When(x => x.Details is not null && x.Role == "Company", () =>
        {
            RuleFor(x => x.Details!.OrganizationLegalName)
                .NotEmpty().MaximumLength(200)
                .WithMessage("Organization legal name is required.");
            RuleFor(x => x.Details!.OrganizationWebsite)
                .NotEmpty().MaximumLength(300)
                .WithMessage("Organization website is required.");
            RuleFor(x => x.Details!.OrganizationEmail)
                .NotEmpty().EmailAddress().MaximumLength(256)
                .WithMessage("A valid organization email is required.");
            RuleFor(x => x.Details!.OrganizationCountry)
                .NotEmpty().MaximumLength(80)
                .WithMessage("Country is required.");
            RuleFor(x => x.Details!.CompanyType)
                .NotEmpty()
                .Must(t => t is not null && AllowedCompanyTypes.Contains(t))
                .WithMessage("Company type must be one of: University, NGO, Company, Foundation, Government, Other.");
            RuleFor(x => x.Details!.CompanyDescription)
                .NotEmpty().MaximumLength(1000)
                .WithMessage("Company description is required (max 1000 characters).");
            RuleFor(x => x.Details!.ContactPersonFullName)
                .NotEmpty().MaximumLength(100)
                .WithMessage("Contact person full name is required.");
            RuleFor(x => x.Details!.ContactPersonPosition)
                .NotEmpty().MaximumLength(100)
                .WithMessage("Contact person position is required.");
            RuleFor(x => x.Details!.ContactPhoneNumber)
                .NotEmpty().MaximumLength(40)
                .Matches(@"^[+0-9 ()\-]{6,40}$")
                .WithMessage("A valid contact phone number is required.");
            RuleFor(x => x.Details!.OrganizationRegistrationNumber)
                .MaximumLength(100);
            RuleFor(x => x.Details!.OrganizationTaxNumber)
                .MaximumLength(100);
        });

        When(x => x.Details is not null && x.Role == "Consultant", () =>
        {
            RuleFor(x => x.Details!.Biography)
                .NotEmpty().MaximumLength(2000)
                .WithMessage("A short bio is required.");
            RuleFor(x => x.Details!.ProfessionalTitle)
                .NotEmpty().MaximumLength(150)
                .WithMessage("Professional title is required.");
            RuleFor(x => x.Details!.HighestDegree)
                .NotEmpty().MaximumLength(150)
                .WithMessage("Highest degree is required.");
            RuleFor(x => x.Details!.FieldOfExpertise)
                .NotEmpty().MaximumLength(200)
                .WithMessage("Field of expertise is required.");
            RuleFor(x => x.Details!.YearsOfExperience)
                .NotNull().GreaterThanOrEqualTo(0).LessThanOrEqualTo(80)
                .WithMessage("Years of experience must be 0 or greater.");
            RuleFor(x => x.Details!.SessionFeeUsd)
                .NotNull().GreaterThan(0)
                .WithMessage("Session fee must be greater than zero.");
            RuleFor(x => x.Details!.SessionDurationMinutes)
                .NotNull()
                .Must(d => d is 30 or 45 or 60 or 90)
                .WithMessage("Session duration must be 30, 45, 60 or 90 minutes.");
            RuleFor(x => x.Details!.ExpertiseTags)
                .NotNull()
                .Must(tags => tags is { Length: > 0 })
                .WithMessage("At least one expertise tag is required.");
            RuleFor(x => x.Details!.Languages)
                .NotNull()
                .Must(langs => langs is { Length: > 0 })
                .WithMessage("At least one language is required.");
            RuleFor(x => x.Details!.Country)
                .NotEmpty().MaximumLength(80)
                .WithMessage("Country is required.");
            RuleFor(x => x.Details!.Timezone)
                .NotEmpty().MaximumLength(64)
                .WithMessage("Time zone is required.");
            RuleFor(x => x.Details!.LinkedInUrl)
                .MaximumLength(2048)
                .Must(u => string.IsNullOrEmpty(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithMessage("LinkedIn URL must be a valid URL.");
            RuleFor(x => x.Details!.PortfolioUrl)
                .MaximumLength(2048)
                .Must(u => string.IsNullOrEmpty(u) || Uri.TryCreate(u, UriKind.Absolute, out _))
                .WithMessage("Portfolio URL must be a valid URL.");
        });
    }
}
