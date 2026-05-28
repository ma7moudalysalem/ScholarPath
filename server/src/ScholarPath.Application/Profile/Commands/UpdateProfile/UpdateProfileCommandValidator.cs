using FluentValidation;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

/// <summary>
/// Full business-rule validation for a profile PATCH. Mirrors the rules the
/// frontend enforces for immediate feedback, then hardens them so a malicious
/// or out-of-date client cannot bypass them (CR-PROF-01..04, CR-PROF-09).
/// </summary>
public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    // GPA scales the platform supports (CR-PROF-02). The selected scale is the
    // upper bound for the GPA value — e.g. "5.0" -> 5.0 max.
    private static readonly IReadOnlyDictionary<string, decimal> GpaScales =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["4.0"]  = 4m,
            ["5.0"]  = 5m,
            ["10.0"] = 10m,
            ["20.0"] = 20m,
            ["100"]  = 100m,
        };

    // CR-PROF-09: consultant sessions only at these durations.
    private static readonly int[] AllowedSessionDurations = [30, 45, 60, 90, 120];

    public UpdateProfileCommandValidator(IDateTimeService clock)
    {
        // ── Identity ─────────────────────────────────────────────────────────
        RuleFor(x => x.Fields.FirstName)
            .NotEmpty().WithMessage("First name cannot be blank.")
            .MaximumLength(100)
            .When(x => x.Fields.FirstName is not null);

        RuleFor(x => x.Fields.LastName)
            .NotEmpty().WithMessage("Last name cannot be blank.")
            .MaximumLength(100)
            .When(x => x.Fields.LastName is not null);

        RuleFor(x => x.Fields.Biography)
            .MaximumLength(2000)
            .When(x => x.Fields.Biography is not null);

        RuleFor(x => x.Fields.CountryOfResidence)
            .MaximumLength(100)
            .When(x => x.Fields.CountryOfResidence is not null);

        RuleFor(x => x.Fields.Nationality)
            .MaximumLength(100)
            .When(x => x.Fields.Nationality is not null);

        RuleFor(x => x.Fields.PreferredLanguage)
            .Must(v => v is null || v == "en" || v == "ar")
            .WithMessage("Preferred language must be 'en' or 'ar'.")
            .When(x => x.Fields.PreferredLanguage is not null);

        // ── Date of birth (CR-PROF-04) ───────────────────────────────────────
        RuleFor(x => x.Fields.DateOfBirth)
            .Must(d => d!.Value <= clock.Today)
            .WithMessage("Date of birth cannot be in the future.")
            .When(x => x.Fields.DateOfBirth.HasValue);

        // ── URL fields (CR-PROF-03) ──────────────────────────────────────────
        RuleFor(x => x.Fields.LinkedInUrl)
            .Must(BeValidAbsoluteHttpUrl)
            .WithMessage("LinkedIn URL must be a valid http(s) URL.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Fields.LinkedInUrl));

        RuleFor(x => x.Fields.WebsiteUrl)
            .Must(BeValidAbsoluteHttpUrl)
            .WithMessage("Website URL must be a valid http(s) URL.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Fields.WebsiteUrl));

        RuleFor(x => x.Fields.OrganizationWebsite)
            .Must(BeValidAbsoluteHttpUrl)
            .WithMessage("Organization website must be a valid http(s) URL.")
            .MaximumLength(500)
            .When(x => !string.IsNullOrWhiteSpace(x.Fields.OrganizationWebsite));

        // ── Academic / Student fields ────────────────────────────────────────
        RuleFor(x => x.Fields.AcademicLevel)
            .Must(v => Enum.TryParse<AcademicLevel>(v, ignoreCase: true, out _))
            .WithMessage("Academic level is not a recognised value.")
            .When(x => !string.IsNullOrWhiteSpace(x.Fields.AcademicLevel));

        RuleFor(x => x.Fields.FieldOfStudy)
            .MaximumLength(200)
            .When(x => x.Fields.FieldOfStudy is not null);

        RuleFor(x => x.Fields.CurrentInstitution)
            .MaximumLength(200)
            .When(x => x.Fields.CurrentInstitution is not null);

        // ── GPA + GpaScale (CR-PROF-02) ──────────────────────────────────────
        RuleFor(x => x.Fields.GpaScale)
            .Must(s => GpaScales.ContainsKey(s!))
            .WithMessage("GPA scale must be one of 4.0, 5.0, 10.0, 20.0 or 100.")
            .When(x => !string.IsNullOrWhiteSpace(x.Fields.GpaScale));

        // If the caller sends a GPA value, GpaScale becomes required.
        RuleFor(x => x.Fields.GpaScale)
            .NotEmpty().WithMessage("GPA scale is required when a GPA is provided.")
            .When(x => x.Fields.Gpa.HasValue);

        RuleFor(x => x.Fields.Gpa)
            .Must((cmd, gpa) => IsGpaWithinScale(gpa!.Value, cmd.Fields.GpaScale))
            .WithMessage("GPA must be between 0 and the selected GPA scale.")
            .When(x => x.Fields.Gpa.HasValue && !string.IsNullOrWhiteSpace(x.Fields.GpaScale));

        // ── Organization / Company (CR-PROF-07) ──────────────────────────────
        RuleFor(x => x.Fields.OrganizationLegalName)
            .NotEmpty().WithMessage("Organization legal name cannot be blank.")
            .MaximumLength(200)
            .When(x => x.Fields.OrganizationLegalName is not null);

        // ── Consultant session settings (CR-PROF-09) ─────────────────────────
        // A fee of 0 marks the consultant's sessions as free (no payment, no
        // commission); only negative values are rejected.
        RuleFor(x => x.Fields.SessionFeeUsd)
            .GreaterThanOrEqualTo(0m).WithMessage("Session fee cannot be negative.")
            .Must(HaveAtMostTwoDecimalPlaces)
            .WithMessage("Session fee can have at most two decimal places.")
            .LessThanOrEqualTo(10000m)
            .WithMessage("Session fee cannot exceed 10,000 USD.")
            .When(x => x.Fields.SessionFeeUsd.HasValue);

        RuleFor(x => x.Fields.SessionDurationMinutes)
            .Must(v => AllowedSessionDurations.Contains(v!.Value))
            .WithMessage("Session duration must be 30, 45, 60, 90 or 120 minutes.")
            .When(x => x.Fields.SessionDurationMinutes.HasValue);

        // ── Consultant professional fields (CR-PROF-08) ──────────────────────
        RuleFor(x => x.Fields.ProfessionalTitle)
            .NotEmpty().WithMessage("Professional title cannot be blank.")
            .MaximumLength(150)
            .When(x => x.Fields.ProfessionalTitle is not null);

        RuleFor(x => x.Fields.YearsOfExperience)
            .InclusiveBetween(0, 70)
            .WithMessage("Years of experience must be between 0 and 70.")
            .When(x => x.Fields.YearsOfExperience.HasValue);

        RuleFor(x => x.Fields.ExpertiseTags)
            .Must(tags => tags!.Count is > 0 and <= 20)
            .WithMessage("Provide between 1 and 20 expertise tags.")
            .When(x => x.Fields.ExpertiseTags is not null);

        RuleForEach(x => x.Fields.ExpertiseTags!)
            .NotEmpty().WithMessage("Expertise tags cannot be blank.")
            .MaximumLength(60).WithMessage("Each expertise tag must be 60 characters or fewer.")
            .When(x => x.Fields.ExpertiseTags is not null);

        RuleFor(x => x.Fields.Languages)
            .Must(languages => languages!.Count is > 0 and <= 20)
            .WithMessage("Provide between 1 and 20 languages.")
            .When(x => x.Fields.Languages is not null);

        RuleForEach(x => x.Fields.Languages!)
            .NotEmpty().WithMessage("Languages cannot be blank.")
            .MaximumLength(60).WithMessage("Each language must be 60 characters or fewer.")
            .When(x => x.Fields.Languages is not null);

        RuleFor(x => x.Fields.Timezone)
            .NotEmpty().WithMessage("Timezone cannot be blank.")
            .MaximumLength(100)
            .Must(BeRecognizedTimezone)
            .WithMessage("Timezone is not a recognised IANA / Windows time zone identifier.")
            .When(x => x.Fields.Timezone is not null);
    }

    private static bool BeValidAbsoluteHttpUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var parsed)
            && (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
    }

    private static bool IsGpaWithinScale(decimal gpa, string? scale)
    {
        if (string.IsNullOrWhiteSpace(scale)) return false;
        if (!GpaScales.TryGetValue(scale, out var max)) return false;
        return gpa >= 0m && gpa <= max;
    }

    private static bool HaveAtMostTwoDecimalPlaces(decimal? value)
    {
        if (!value.HasValue) return true;
        // round to two places and compare — equality means no precision was lost.
        var rounded = Math.Round(value.Value, 2, MidpointRounding.AwayFromZero);
        return value.Value == rounded;
    }

    private static bool BeRecognizedTimezone(string? tz)
    {
        if (string.IsNullOrWhiteSpace(tz)) return true;
        try
        {
            // FindSystemTimeZoneById accepts IANA on .NET 10 (cross-platform).
            _ = TimeZoneInfo.FindSystemTimeZoneById(tz);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
        catch (Exception)
        {
            // Defensive — never throw out of a validator.
            return false;
        }
    }

}
