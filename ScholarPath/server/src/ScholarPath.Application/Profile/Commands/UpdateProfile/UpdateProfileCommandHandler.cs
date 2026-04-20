using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Profile.Commands.UpdateProfile;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileDto>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public UpdateProfileCommandHandler(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task<UserProfileDto> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new UnauthorizedAccessException("errors.auth.userNotFound");

        // Update user fields
        if (request.FirstName is not null) user.FirstName = request.FirstName;
        if (request.LastName is not null) user.LastName = request.LastName;

        await _userManager.UpdateAsync(user);

        // Update or create profile
        var profile = await _dbContext.UserProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

        if (profile is null)
        {
            profile = new UserProfile { UserId = userId };
            _dbContext.UserProfiles.Add(profile);
        }

        if (request.FieldOfStudy is not null) profile.FieldOfStudy = request.FieldOfStudy;
        if (request.GPA is not null) profile.GPA = request.GPA;
        if (request.Interests is not null) profile.Interests = request.Interests;
        if (request.Country is not null) profile.Country = request.Country;
        if (request.TargetCountry is not null) profile.TargetCountry = request.TargetCountry;
        if (request.Bio is not null) profile.Bio = request.Bio;
        if (request.DateOfBirth is not null) profile.DateOfBirth = request.DateOfBirth;

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Calculate completeness and return
        var completeness = CalculateCompleteness(user, profile);

        return new UserProfileDto(
            user.Id,
            user.FirstName,
            user.LastName,
            user.Email!,
            user.ProfileImageUrl,
            profile.FieldOfStudy,
            profile.GPA,
            profile.Interests,
            profile.Country,
            profile.TargetCountry,
            profile.Bio,
            profile.DateOfBirth,
            completeness);
    }

    private static int CalculateCompleteness(ApplicationUser user, UserProfile? profile)
    {
        var total = 0;
        var filled = 0;

        total += 3;
        if (!string.IsNullOrEmpty(user.FirstName)) filled++;
        if (!string.IsNullOrEmpty(user.LastName)) filled++;
        if (!string.IsNullOrEmpty(user.ProfileImageUrl)) filled++;

        total += 6;
        if (!string.IsNullOrEmpty(profile?.FieldOfStudy)) filled++;
        if (profile?.GPA is not null) filled++;
        if (!string.IsNullOrEmpty(profile?.Country)) filled++;
        if (!string.IsNullOrEmpty(profile?.TargetCountry)) filled++;
        if (!string.IsNullOrEmpty(profile?.Bio)) filled++;
        if (profile?.DateOfBirth is not null) filled++;

        return total == 0 ? 0 : (int)Math.Round((double)filled / total * 100);
    }
}
