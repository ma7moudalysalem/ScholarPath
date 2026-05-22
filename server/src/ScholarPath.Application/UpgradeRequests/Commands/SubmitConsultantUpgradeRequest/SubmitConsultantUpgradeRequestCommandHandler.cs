using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgradeRequest;

/// <summary>
/// Persists a Consultant upgrade submission so it shows up in the Admin
/// upgrade queue. Side-effects: writes the submitted consultant profile fields
/// onto the user's <see cref="UserProfile"/> (Student fields are untouched —
/// the schemas don't overlap) so the reviewer sees the proposed profile, and
/// fills <see cref="ApplicationUser.CountryOfResidence"/> if it's blank.
/// </summary>
public sealed class SubmitConsultantUpgradeRequestCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAdministration userAdministration,
    IDateTimeService clock,
    ILogger<SubmitConsultantUpgradeRequestCommandHandler> logger)
    : IRequestHandler<SubmitConsultantUpgradeRequestCommand, Guid>
{
    public async Task<Guid> Handle(SubmitConsultantUpgradeRequestCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        // Only an active account can request an upgrade — suspended / pending
        // users have other things to resolve first.
        if (user.AccountStatus != AccountStatus.Active)
        {
            throw new ConflictException(
                "Your account must be active before requesting a consultant upgrade.");
        }

        var roles = await userAdministration.GetRolesAsync(userId, ct).ConfigureAwait(false);
        if (!roles.Contains("Student"))
        {
            throw new ForbiddenAccessException(
                "Only active Students can request a consultant upgrade.");
        }
        if (roles.Contains("Consultant"))
        {
            throw new ConflictException("You are already a Consultant.");
        }

        // One outstanding consultant request at a time — if it's still Pending
        // the admin will see the latest fields by editing the profile, not by
        // stacking duplicate queue entries.
        var existingPending = await db.UpgradeRequests
            .AnyAsync(r => r.UserId == userId
                          && r.Target == UpgradeTarget.Consultant
                          && r.Status == UpgradeRequestStatus.Pending
                          && !r.IsDeleted, ct)
            .ConfigureAwait(false);
        if (existingPending)
        {
            throw new ConflictException(
                "You already have a pending consultant upgrade request.");
        }

        // Stamp the submitted consultant fields on the profile so the admin
        // reviews a complete picture. Student-only fields (AcademicLevel,
        // FieldOfStudy, …) are not touched.
        if (user.Profile is null)
        {
            var newProfile = new UserProfile
            {
                UserId = userId,
                CreatedAt = clock.UtcNow,
            };
            db.UserProfiles.Add(newProfile);
            user.Profile = newProfile;
        }

        user.Profile.Biography = request.Biography;
        user.Profile.ProfessionalTitle = request.ProfessionalTitle;
        user.Profile.HighestDegree = request.HighestDegree;
        user.Profile.FieldOfExpertise = request.FieldOfExpertise;
        user.Profile.YearsOfExperience = request.YearsOfExperience;
        user.Profile.SessionFeeUsd = request.SessionFeeUsd;
        user.Profile.SessionDurationMinutes = request.SessionDurationMinutes ?? 45;
        user.Profile.ExpertiseTagsJson = request.ExpertiseTags is { Length: > 0 } tags
            ? JsonSerializer.Serialize(tags)
            : null;
        user.Profile.LanguagesJson = request.Languages is { Length: > 0 } langs
            ? JsonSerializer.Serialize(langs)
            : null;
        user.Profile.Timezone = request.Timezone;
        user.Profile.LinkedInUrl = request.LinkedInUrl;
        user.Profile.PortfolioUrl = request.PortfolioUrl;
        // Don't clobber an existing Student-set country.
        if (string.IsNullOrWhiteSpace(user.CountryOfResidence))
        {
            user.CountryOfResidence = request.Country;
        }

        var upgradeRequest = new UpgradeRequest
        {
            UserId = userId,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Pending,
            Reason = "Student-initiated consultant upgrade.",
            CreatedAt = clock.UtcNow,
        };
        db.UpgradeRequests.Add(upgradeRequest);

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "User {UserId} submitted consultant upgrade request {RequestId}.",
            userId, upgradeRequest.Id);

        return upgradeRequest.Id;
    }
}
