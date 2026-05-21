using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

public sealed class UpdateProfileCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    // Status values that mean the company has already cleared admin verification.
    // Once verified, legal-name and website edits go through a re-verification
    // flow (CR-PROF-07) — the handler resets the status to PendingReview so the
    // admin sees the change before it lands on the public record.
    private const string OrgVerifiedStatus = "Verified";
    private const string OrgPendingReviewStatus = "PendingReview";

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        var profile = user.Profile;
        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            db.UserProfiles.Add(profile);
            user.Profile = profile;
        }

        var f = request.Fields;
        // NOTE: Role, ActiveRole, AccountStatus, OrganizationVerificationStatus
        // and similar fields are intentionally not on UpdateProfileRequestDto —
        // a profile edit can never escalate or change a user's standing
        // (CR-PROF-11 — mass-assignment defence).
        if (f.FirstName is not null) user.FirstName = f.FirstName.Trim();
        if (f.LastName is not null) user.LastName = f.LastName.Trim();
        if (f.CountryOfResidence is not null) user.CountryOfResidence = f.CountryOfResidence;
        if (f.PreferredLanguage is not null) user.PreferredLanguage = f.PreferredLanguage;

        if (f.Biography is not null) profile.Biography = f.Biography;
        if (f.DateOfBirth is not null) profile.DateOfBirth = f.DateOfBirth;
        if (f.Nationality is not null) profile.Nationality = f.Nationality;
        if (f.LinkedInUrl is not null) profile.LinkedInUrl = string.IsNullOrWhiteSpace(f.LinkedInUrl) ? null : f.LinkedInUrl.Trim();
        if (f.WebsiteUrl is not null) profile.WebsiteUrl = string.IsNullOrWhiteSpace(f.WebsiteUrl) ? null : f.WebsiteUrl.Trim();
        if (f.FieldOfStudy is not null) profile.FieldOfStudy = f.FieldOfStudy;
        if (f.CurrentInstitution is not null) profile.CurrentInstitution = f.CurrentInstitution;
        if (f.Gpa is not null) profile.Gpa = f.Gpa;
        if (f.GpaScale is not null) profile.GpaScale = string.IsNullOrWhiteSpace(f.GpaScale) ? null : f.GpaScale;

        // CR-PROF-07: changing approved legal/verification-sensitive data
        // resets the company back to PendingReview so the admin can re-verify
        // the new values before they appear on the public record.
        var orgLegalNameChanged = f.OrganizationLegalName is not null
            && !string.Equals(profile.OrganizationLegalName, f.OrganizationLegalName, StringComparison.Ordinal);
        var orgWebsiteChanged = f.OrganizationWebsite is not null
            && !string.Equals(profile.OrganizationWebsite, f.OrganizationWebsite, StringComparison.Ordinal);
        if (f.OrganizationLegalName is not null) profile.OrganizationLegalName = f.OrganizationLegalName.Trim();
        if (f.OrganizationWebsite is not null) profile.OrganizationWebsite = string.IsNullOrWhiteSpace(f.OrganizationWebsite) ? null : f.OrganizationWebsite.Trim();
        if ((orgLegalNameChanged || orgWebsiteChanged)
            && string.Equals(profile.OrganizationVerificationStatus, OrgVerifiedStatus, StringComparison.Ordinal))
        {
            profile.OrganizationVerificationStatus = OrgPendingReviewStatus;
            profile.OrganizationVerifiedAt = null;
        }

        if (f.SessionFeeUsd is not null) profile.SessionFeeUsd = f.SessionFeeUsd;
        if (f.SessionDurationMinutes is not null) profile.SessionDurationMinutes = f.SessionDurationMinutes;

        // Consultant professional fields (CR-PROF-08).
        if (f.ProfessionalTitle is not null) profile.ProfessionalTitle = string.IsNullOrWhiteSpace(f.ProfessionalTitle) ? null : f.ProfessionalTitle.Trim();
        if (f.YearsOfExperience is not null) profile.YearsOfExperience = f.YearsOfExperience;
        if (f.ExpertiseTags is not null) profile.ExpertiseTagsJson = SerializeStringList(f.ExpertiseTags);
        if (f.Languages is not null) profile.LanguagesJson = SerializeStringList(f.Languages);
        if (f.Timezone is not null) profile.Timezone = string.IsNullOrWhiteSpace(f.Timezone) ? null : f.Timezone.Trim();

        if (f.AcademicLevel is not null
            && Enum.TryParse<AcademicLevel>(f.AcademicLevel, ignoreCase: true, out var level))
        {
            profile.AcademicLevel = level;
        }

        profile.ProfileCompletenessPercent = ProfileCompletenessCalculator.Calculate(user, profile);
        await db.SaveChangesAsync(ct);

        return ProfileMapper.ToDto(user, profile);
    }

    private static string? SerializeStringList(IReadOnlyCollection<string> values)
    {
        var cleaned = values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();
        return cleaned.Count == 0 ? null : JsonSerializer.Serialize(cleaned);
    }
}
