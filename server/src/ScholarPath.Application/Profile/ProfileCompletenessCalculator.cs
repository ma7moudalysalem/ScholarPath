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
        // Common fields count toward completeness for every role.
        var fields = new List<object?>
        {
            Blank(user.FirstName),
            Blank(user.LastName),
            Blank(user.ProfileImageUrl),
            Blank(user.CountryOfResidence),
            profile?.Biography is { Length: > 0 } ? profile.Biography : null,
            profile?.Nationality,
            profile?.DateOfBirth,
            profile?.LinkedInUrl,
            profile?.WebsiteUrl,
        };

        // Academic background only applies to Students — counting it for a
        // ScholarshipProvider or Consultant would cap their meter below 100%
        // forever (they have no academic fields to fill). FR-PROF-09/08.
        if (string.Equals(user.ActiveRole, "Student", StringComparison.OrdinalIgnoreCase))
        {
            fields.Add(profile?.FieldOfStudy);
            fields.Add(profile?.AcademicLevel);
            fields.Add(profile?.CurrentInstitution);
        }

        var filled = fields.Count(f => f is not null);
        return (int)Math.Round(filled * 100.0 / fields.Count, MidpointRounding.AwayFromZero);
    }

    private static string? Blank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
