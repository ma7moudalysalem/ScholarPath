using FluentValidation;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandValidator : AbstractValidator<SelectRoleCommand>
{
    // Allowed Company Type values — kept as a string set so we don't ripple a new enum
    // through migrations and the snapshot. The frontend mirrors this list.
    private static readonly string[] AllowedCompanyTypes =
        ["University", "NGO", "Company", "Foundation", "Government", "Other"];

    /// <summary>
    /// Canonical list of allowed consultant session durations (minutes). Shared
    /// between the Auth/onboarding flow and the Profile module so the frontend
    /// only ever needs to read one list (AUTH-CODE-04).
    /// </summary>
    public static readonly int[] AllowedSessionDurations = [30, 45, 60, 90, 120];

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
            // AUTH-CODE-04: website must be a valid absolute URL — non-empty alone
            // is not enough; an https://… URL is what the SRS specifies.
            RuleFor(x => x.Details!.OrganizationWebsite)
                .NotEmpty().MaximumLength(300)
                .Must(u => !string.IsNullOrWhiteSpace(u)
                    && Uri.TryCreate(u, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                .WithMessage("Organization website must be a valid absolute URL (http:// or https://).");
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
            // AUTH-CODE-04: SRS says 2000 characters (was 1000 in code).
            RuleFor(x => x.Details!.CompanyDescription)
                .NotEmpty().MaximumLength(2000)
                .WithMessage("Company description is required (max 2000 characters).");
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

            // AUTH-CODE-03: conditional applicability — if the Company says they
            // are NOT tax-registered, they owe an explanation. Same for legal
            // registration. If they ARE registered, the number must be present.
            RuleFor(x => x.Details!.TaxNotApplicableReason)
                .NotEmpty().MaximumLength(500)
                .When(x => x.Details!.IsTaxRegistered == false)
                .WithMessage("Tell us why a tax registration does not apply (e.g. not-for-profit, unincorporated).");
            RuleFor(x => x.Details!.OrganizationTaxNumber)
                .NotEmpty()
                .When(x => x.Details!.IsTaxRegistered == true)
                .WithMessage("A tax registration number is required when the organization is tax-registered.");
            RuleFor(x => x.Details!.LegalRegistrationNotApplicableReason)
                .NotEmpty().MaximumLength(500)
                .When(x => x.Details!.IsLegallyRegistered == false)
                .WithMessage("Tell us why a legal registration does not apply.");
            RuleFor(x => x.Details!.OrganizationRegistrationNumber)
                .NotEmpty()
                .When(x => x.Details!.IsLegallyRegistered == true)
                .WithMessage("A business registration number is required when the organization is legally registered.");
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
            // AUTH-CODE-04: SRS says years of experience >= 1 (was >= 0).
            RuleFor(x => x.Details!.YearsOfExperience)
                .NotNull().GreaterThanOrEqualTo(1).LessThanOrEqualTo(80)
                .WithMessage("Years of experience must be at least 1.");
            RuleFor(x => x.Details!.SessionFeeUsd)
                .NotNull().GreaterThan(0)
                .WithMessage("Session fee must be greater than zero.");
            // AUTH-CODE-04: canonical session-duration list shared with Profile.
            RuleFor(x => x.Details!.SessionDurationMinutes)
                .NotNull()
                .Must(d => d is not null && AllowedSessionDurations.Contains(d.Value))
                .WithMessage("Session duration must be one of 30, 45, 60, 90, or 120 minutes.");
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
