using System.Text.Json;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Profile;

internal static class ProfileMapper
{
    public static UserProfileDto ToDto(ApplicationUser user, UserProfile? profile) =>
        new(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.ProfileImageUrl,
            user.AccountStatus.ToString(),
            user.CountryOfResidence,
            user.PreferredLanguage,
            profile?.Biography,
            profile?.DateOfBirth,
            profile?.Nationality,
            profile?.LinkedInUrl,
            profile?.WebsiteUrl,
            profile?.AcademicLevel?.ToString(),
            profile?.FieldOfStudy,
            profile?.CurrentInstitution,
            profile?.Gpa,
            profile?.GpaScale,
            profile?.OrganizationLegalName,
            profile?.OrganizationWebsite,
            profile?.OrganizationVerificationStatus,
            profile?.SessionFeeUsd,
            profile?.SessionDurationMinutes,
            // Consultant professional fields (CR-PROF-08)
            profile?.ProfessionalTitle,
            profile?.YearsOfExperience,
            ParseStringList(profile?.ExpertiseTagsJson),
            ParseStringList(profile?.LanguagesJson),
            profile?.Timezone,
            // Student matching inputs — preferred study destinations + fields.
            ParseStringList(profile?.PreferredCountriesJson),
            ParseStringList(profile?.PreferredFieldsJson),
            profile?.ProfileCompletenessPercent ?? ProfileCompletenessCalculator.Calculate(user, profile),
            // CR-PROF-06: empty password hash means the user signs in via SSO only.
            !string.IsNullOrEmpty(user.PasswordHash));

    private static IReadOnlyCollection<string>? ParseStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var values = JsonSerializer.Deserialize<List<string>>(json);
            return values is { Count: > 0 } ? values : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
