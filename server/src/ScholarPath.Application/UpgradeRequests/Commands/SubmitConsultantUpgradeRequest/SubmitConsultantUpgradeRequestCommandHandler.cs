using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
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
    INotificationDispatcher notifications,
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

        // GAP-1 / FR-ONB-08 — a Student-initiated consultant upgrade must clear the
        // SAME document bar as a fresh Consultant onboarding (ConsultantIdentityProof,
        // ConsultantDegreeCertificate, ConsultantCvResume). SelectRoleCommandHandler
        // enforces this for first-time onboarding; the upgrade path previously enforced
        // only the profile fields, letting a request reach the admin queue with no
        // supporting documents. Mirror the defensive minimum-count check here.
        const int RequiredConsultantDocs = 3;
        var onboardingDocCount = await db.Documents
            .Where(d => d.OwnerUserId == userId
                        && d.Category == DocumentCategory.OnboardingDocument)
            .CountAsync(ct).ConfigureAwait(false);
        if (onboardingDocCount < RequiredConsultantDocs)
        {
            var missing = RequiredConsultantDocs - onboardingDocCount;
            throw new ConflictException(
                $"Upload {RequiredConsultantDocs} verification document(s) before requesting a consultant upgrade — "
                + $"{missing} more required.");
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

        // Master switch: when payments are off, force the requested session
        // fee to 0 silently regardless of what the upgrade request set.
        var paymentsEnabled = await PlatformSettingsReader.GetBooleanAsync(
            db, PlatformSettingsKeys.PaymentsEnabled, defaultValue: true, ct);
        var effectiveSessionFee = paymentsEnabled ? request.SessionFeeUsd : 0m;

        if (paymentsEnabled && request.SessionFeeUsd == 0m)
        {
            var freeAllowed = await PlatformSettingsReader.GetBooleanAsync(
                db, PlatformSettingsKeys.AllowFreeConsultantSessions, defaultValue: true, ct);
            if (!freeAllowed)
                throw new ConflictException(
                    "Free consultant sessions are not enabled on this platform. Please set a Session Fee greater than 0.");
        }

        user.Profile.Biography = request.Biography;
        user.Profile.ProfessionalTitle = request.ProfessionalTitle;
        user.Profile.HighestDegree = request.HighestDegree;
        user.Profile.FieldOfExpertise = request.FieldOfExpertise;
        user.Profile.YearsOfExperience = request.YearsOfExperience;
        user.Profile.SessionFeeUsd = effectiveSessionFee;
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

        // Surface the new request in the admin upgrade queue. Best-effort:
        // a notification failure must never break the submission itself.
        var studentName = $"{user.FirstName} {user.LastName}".Trim();
        await NotifyAdminsAsync(
            string.IsNullOrWhiteSpace(studentName) ? user.Email ?? "A student" : studentName,
            upgradeRequest.Id, ct);

        logger.LogInformation(
            "User {UserId} submitted consultant upgrade request {RequestId}.",
            userId, upgradeRequest.Id);

        return upgradeRequest.Id;
    }

    // FR-ONB-07 — alert every admin so a new upgrade request never sits unseen.
    private async Task NotifyAdminsAsync(string studentName, Guid requestId, CancellationToken ct)
    {
        try
        {
            var adminIds = await db.Users
                .Where(u => (u.ActiveRole == "Admin" || u.ActiveRole == "SuperAdmin")
                            && u.AccountStatus == AccountStatus.Active)
                .Select(u => u.Id)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            if (adminIds.Count == 0) return;

            foreach (var adminId in adminIds)
            {
                await notifications.DispatchAsync(
                    adminId,
                    NotificationType.UpgradeRequestSubmitted,
                    new NotificationParams { CounterpartyName = studentName, StatusText = "Consultant" },
                    deepLink: "/admin/upgrades",
                    idempotencyKey: $"upgrade-submitted:{requestId:N}:{adminId:N}",
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to notify admins of upgrade request {RequestId}.", requestId);
        }
    }
}
