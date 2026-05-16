using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Profile;

/// <summary>
/// Server-side profile-completeness meter (PB-002 acceptance #3). Counts the
/// share of core profile fields that are filled in.
/// </summary>
public static class ProfileCompletenessCalculator
{
    public static int Calculate(ApplicationUser user, UserProfile? profile)
    {
        object?[] fields =
        [
            Blank(user.FirstName),
            Blank(user.LastName),
            Blank(user.ProfileImageUrl),
            Blank(user.CountryOfResidence),
            profile?.Biography is { Length: > 0 } ? profile.Biography : null,
            profile?.Nationality,
            profile?.DateOfBirth,
            profile?.FieldOfStudy,
            profile?.AcademicLevel,
            profile?.CurrentInstitution,
            profile?.LinkedInUrl,
            profile?.WebsiteUrl,
        ];

        var filled = fields.Count(f => f is not null);
        return (int)Math.Round(filled * 100.0 / fields.Length, MidpointRounding.AwayFromZero);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
