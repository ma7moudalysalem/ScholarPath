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
            profile?.ProfileCompletenessPercent ?? ProfileCompletenessCalculator.Calculate(user, profile));
}
