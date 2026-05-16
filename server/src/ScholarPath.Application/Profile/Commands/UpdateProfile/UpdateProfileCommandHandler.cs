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
        if (f.FirstName is not null) user.FirstName = f.FirstName.Trim();
        if (f.LastName is not null) user.LastName = f.LastName.Trim();
        if (f.CountryOfResidence is not null) user.CountryOfResidence = f.CountryOfResidence;
        if (f.PreferredLanguage is not null) user.PreferredLanguage = f.PreferredLanguage;

        if (f.Biography is not null) profile.Biography = f.Biography;
        if (f.DateOfBirth is not null) profile.DateOfBirth = f.DateOfBirth;
        if (f.Nationality is not null) profile.Nationality = f.Nationality;
        if (f.LinkedInUrl is not null) profile.LinkedInUrl = f.LinkedInUrl;
        if (f.WebsiteUrl is not null) profile.WebsiteUrl = f.WebsiteUrl;
        if (f.FieldOfStudy is not null) profile.FieldOfStudy = f.FieldOfStudy;
        if (f.CurrentInstitution is not null) profile.CurrentInstitution = f.CurrentInstitution;
        if (f.Gpa is not null) profile.Gpa = f.Gpa;
        if (f.GpaScale is not null) profile.GpaScale = f.GpaScale;
        if (f.OrganizationLegalName is not null) profile.OrganizationLegalName = f.OrganizationLegalName;
        if (f.OrganizationWebsite is not null) profile.OrganizationWebsite = f.OrganizationWebsite;
        if (f.SessionFeeUsd is not null) profile.SessionFeeUsd = f.SessionFeeUsd;
        if (f.SessionDurationMinutes is not null) profile.SessionDurationMinutes = f.SessionDurationMinutes;
        if (f.AcademicLevel is not null
            && Enum.TryParse<AcademicLevel>(f.AcademicLevel, ignoreCase: true, out var level))
        {
            profile.AcademicLevel = level;
        }

        profile.ProfileCompletenessPercent = ProfileCompletenessCalculator.Calculate(user, profile);
        await db.SaveChangesAsync(ct);

        return ProfileMapper.ToDto(user, profile);
    }
}
