using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Documents;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Auth.Commands.SelectRole;

public sealed class SelectRoleCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IUserAdministration userAdministration,
    ITokenService tokenService,
    INotificationDispatcher notifications,
    ILogger<SelectRoleCommandHandler> logger)
    : IRequestHandler<SelectRoleCommand, AuthTokensDto>
{
    // FR-ONB-12/13 (submission gate): before an applicant lands in the admin queue
    // they must have uploaded the SPECIFIC verification documents their role requires.
    // The onboarding wizard uploads typed documents on the documents step BEFORE this
    // submit; the required-type set lives in OnboardingDocumentRequirements. The admin
    // approval handler re-checks the same set as a backstop (defense-in-depth).
    public async Task<AuthTokensDto> Handle(SelectRoleCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var user = await db.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new NotFoundException(nameof(ApplicationUser), userId);

        // FR-AUTH-05 — email verification is REQUIRED before onboarding. An
        // unverified account cannot select a role until it confirms its email.
        if (!user.EmailConfirmed)
            throw new ConflictException("Please verify your email address before continuing.");

        // Role selection is a one-time gate — only an Unassigned, role-less account qualifies.
        var existingRoles = await userAdministration.GetRolesAsync(userId, ct);
        if (existingRoles.Count > 0 || user.AccountStatus != AccountStatus.Unassigned)
            throw new ConflictException("A role has already been selected for this account.");

        if (request.Role == "Student")
        {
            // Students need no admin review — grant the role and activate immediately.
            await userAdministration.AddRoleAsync(userId, "Student", ct);
            user.ActiveRole = "Student";
            user.AccountStatus = AccountStatus.Active;
            user.IsOnboardingComplete = true;
        }
        else
        {
            // FR-ONB-13 — require the specific mandatory verification document TYPES
            // (not merely a count). Only typed, undeleted OnboardingDocument uploads count.
            var uploadedTypes = await db.Documents
                .Where(d => d.OwnerUserId == userId
                            && d.Category == DocumentCategory.OnboardingDocument
                            && d.OnboardingType != null)
                .Select(d => d.OnboardingType!.Value)
                .ToListAsync(ct).ConfigureAwait(false);

            var missingTypes = OnboardingDocumentRequirements.MissingTypes(request.Role, uploadedTypes);
            if (missingTypes.Count > 0)
            {
                throw new ConflictException(
                    "Upload the required verification documents before submitting your onboarding request. Missing: "
                    + string.Join(", ", missingTypes) + ".");
            }

            // ScholarshipProvider / Consultant must be vetted — park them in the onboarding queue.
            // ActiveRole carries the requested role (the queue surfaces it); the Identity
            // role itself is granted by ReviewOnboardingCommandHandler on approval.
            user.ActiveRole = request.Role;
            user.AccountStatus = AccountStatus.PendingApproval;

            // Persist the onboarding profile details so the admin reviews a
            // complete request, not a bare role pick.
            if (request.Details is { } details)
            {
                if (user.Profile is null)
                {
                    // Explicit Add — relying on navigation fix-up alone is fragile
                    // (the InMemory provider can mis-track the entity, surfacing as
                    // DbUpdateConcurrencyException on SaveChanges).
                    var newProfile = new UserProfile
                    {
                        UserId = userId,
                        CreatedAt = DateTimeOffset.UtcNow,
                    };
                    db.UserProfiles.Add(newProfile);
                    user.Profile = newProfile;
                }

                if (request.Role == "ScholarshipProvider")
                {
                    user.Profile.OrganizationLegalName = details.OrganizationLegalName;
                    user.Profile.OrganizationWebsite = details.OrganizationWebsite;
                    user.Profile.OrganizationEmail = details.OrganizationEmail;
                    user.Profile.OrganizationCountry = details.OrganizationCountry;
                    user.Profile.ScholarshipProviderType = details.ScholarshipProviderType;
                    user.Profile.ScholarshipProviderDescription = details.ScholarshipProviderDescription;
                    user.Profile.OrganizationRegistrationNumber = details.OrganizationRegistrationNumber;
                    user.Profile.OrganizationTaxNumber = details.OrganizationTaxNumber;
                    user.Profile.ContactPersonFullName = details.ContactPersonFullName;
                    user.Profile.ContactPersonPosition = details.ContactPersonPosition;
                    user.Profile.ContactPhoneNumber = details.ContactPhoneNumber;
                    user.Profile.OrganizationVerificationStatus = "Pending";
                    // AUTH-CODE-03 — conditional applicability fields.
                    user.Profile.IsTaxRegistered = details.IsTaxRegistered;
                    user.Profile.TaxNotApplicableReason = details.TaxNotApplicableReason;
                    user.Profile.IsLegallyRegistered = details.IsLegallyRegistered;
                    user.Profile.LegalRegistrationNotApplicableReason =
                        details.LegalRegistrationNotApplicableReason;
                    // AUTH-CODE-06 — once the applicant resubmits, the stored
                    // rejection feedback is stale. Clear it so the wizard does
                    // not keep showing the previous reason after they have
                    // already acted on it.
                    user.Profile.LastOnboardingRejectionReason = null;
                    user.Profile.LastOnboardingRejectedAt = null;
                }
                else
                {
                    // Master switch: when payments are off platform-wide,
                    // force the session fee to 0 silently.
                    var paymentsEnabled = await PlatformSettingsReader.GetBooleanAsync(
                        db, PlatformSettingsKeys.PaymentsEnabled, defaultValue: true, ct);
                    var effectiveSessionFee = paymentsEnabled ? details.SessionFeeUsd : 0m;

                    if (paymentsEnabled && details.SessionFeeUsd == 0m)
                    {
                        var freeAllowed = await PlatformSettingsReader.GetBooleanAsync(
                            db, PlatformSettingsKeys.AllowFreeConsultantSessions, defaultValue: true, ct);
                        if (!freeAllowed)
                            throw new ConflictException(
                                "Free consultant sessions are not enabled on this platform. Please set a Session Fee greater than 0.");
                    }

                    user.Profile.Biography = details.Biography;
                    user.Profile.ProfessionalTitle = details.ProfessionalTitle;
                    user.Profile.HighestDegree = details.HighestDegree;
                    user.Profile.FieldOfExpertise = details.FieldOfExpertise;
                    user.Profile.YearsOfExperience = details.YearsOfExperience;
                    user.Profile.SessionFeeUsd = effectiveSessionFee;
                    user.Profile.SessionDurationMinutes = details.SessionDurationMinutes ?? 45;
                    user.Profile.ExpertiseTagsJson = details.ExpertiseTags is { Length: > 0 } tags
                        ? JsonSerializer.Serialize(tags)
                        : null;
                    user.Profile.LanguagesJson = details.Languages is { Length: > 0 } langs
                        ? JsonSerializer.Serialize(langs)
                        : null;
                    user.Profile.Timezone = details.Timezone;
                    user.Profile.LinkedInUrl = details.LinkedInUrl;
                    user.Profile.PortfolioUrl = details.PortfolioUrl;
                    if (!string.IsNullOrWhiteSpace(details.Country))
                        user.CountryOfResidence = details.Country;
                    // AUTH-CODE-06 — clear stale rejection feedback on resubmission.
                    user.Profile.LastOnboardingRejectionReason = null;
                    user.Profile.LastOnboardingRejectedAt = null;
                }
            }
        }

        await db.SaveChangesAsync(ct);

        // ScholarshipProvider/Consultant just landed in the onboarding queue — let the admins
        // know there's something to review. Best-effort: never break role selection.
        if (user.AccountStatus == AccountStatus.PendingApproval)
        {
            await NotifyAdminsAsync(BuildDisplayName(user), request.Role, userId, ct);
        }

        // Kill any session still carrying the old role-less JWT.
        await tokenService.RevokeAllForUserAsync(userId, "Role selected", ct);

        var roles = await userAdministration.GetRolesAsync(userId, ct);
        var tokens = tokenService.IssueTokens(user, roles, user.ActiveRole, rememberMe: false);

        logger.LogInformation(
            "User {UserId} selected role {Role} -> account status {Status}.",
            userId, request.Role, user.AccountStatus);

        return AuthDtoFactory.Build(tokens, user, roles);
    }

    // FR-ONB — alert every admin so a new onboarding request never sits unseen.
    // Best-effort: a notification-channel failure must never break role selection.
    private async Task NotifyAdminsAsync(
        string applicantName, string requestedRole, Guid applicantId, CancellationToken ct)
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
                    NotificationType.OnboardingSubmitted,
                    new NotificationParams { CounterpartyName = applicantName, StatusText = requestedRole },
                    deepLink: "/admin/onboarding",
                    idempotencyKey: $"onboarding-submitted:{applicantId:N}:{adminId:N}",
                    ct).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Failed to notify admins of onboarding submission by {ApplicantId}.", applicantId);
        }
    }

    private static string BuildDisplayName(ApplicationUser user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        if (!string.IsNullOrWhiteSpace(name)) return name;
        return user.Profile?.OrganizationLegalName
            ?? user.Email
            ?? "An applicant";
    }
}
