using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface IDataDeleteJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Processes Delete requests whose cooling-off period has elapsed.
///
/// GDPR Art. 17 (right to erasure): personal data must be removed or
/// irreversibly anonymised across EVERY table — not just the User row.
/// Records that must legally be retained (financial / payment rows) keep the
/// transaction but have their PII-bearing link anonymised: the user row itself
/// is anonymised so the foreign key no longer resolves to a real person.
///
/// Recurring daily.
/// </summary>
public sealed class DataDeleteJob(
    ApplicationDbContext db,
    IDateTimeService clock,
    ILogger<DataDeleteJob> logger) : IDataDeleteJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;

        var due = await db.UserDataRequests
            .Where(r => r.Type == UserDataRequestType.Delete
                && r.Status == UserDataRequestStatus.Pending
                && r.ScheduledProcessAt != null
                && r.ScheduledProcessAt <= now)
            .OrderBy(r => r.ScheduledProcessAt)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (due.Count == 0) return;

        logger.LogInformation("[job] DataDelete processing {Count} due deletions", due.Count);

        foreach (var req in due)
        {
            try
            {
                req.Status = UserDataRequestStatus.Processing;
                req.UpdatedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                await AnonymiseUserAsync(req.UserId, now, ct).ConfigureAwait(false);

                // Audit the erasure event itself (GDPR accountability — Art. 5(2)).
                db.AuditLogs.Add(new Domain.Entities.AuditLog
                {
                    ActorUserId = null, // system job
                    Action = AuditAction.Delete,
                    TargetType = "UserDataRequest",
                    TargetId = req.Id,
                    Summary = $"Account erasure completed for user {req.UserId}",
                    OccurredAt = now,
                });

                req.Status = UserDataRequestStatus.Completed;
                req.CompletedAt = now;
                req.UpdatedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[job] DataDelete failed for request {Id}", req.Id);
                req.Status = UserDataRequestStatus.Failed;
                req.FailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                req.UpdatedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>
    /// Removes or irreversibly anonymises every piece of personal data the
    /// platform holds for <paramref name="userId"/>. Safe to re-run: each step
    /// is idempotent (overwrites with the same anonymised value / deletes again).
    /// IgnoreQueryFilters is used everywhere so a partially-completed previous
    /// run (soft-deleted rows) is still picked up.
    /// </summary>
    private async Task AnonymiseUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        // ── User + profile ──────────────────────────────────────────────────
        var user = await db.Users
            .IgnoreQueryFilters()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            .ConfigureAwait(false);

        if (user is null)
        {
            // Nothing to erase — user already gone. Still idempotent: a re-run
            // of the related-row sweep below is a no-op.
            logger.LogWarning("[job] DataDelete: user {UserId} not found, nothing to erase", userId);
            return;
        }

        user.IsDeleted = true;
        user.DeletedAt = now;
        user.DeletedByUserId = null; // system action — not the user themselves
        user.Email = $"deleted+{user.Id}@scholarpath.invalid";
        user.UserName = user.Email;
        user.NormalizedEmail = user.Email.ToUpperInvariant();
        user.NormalizedUserName = user.NormalizedEmail;
        user.FirstName = "Deleted";
        user.LastName = "User";
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.ProfileImageUrl = null;
        user.PasswordHash = null;            // credentials must not survive erasure
        user.SecurityStamp = Guid.NewGuid().ToString();
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.TwoFactorEnabled = false;
        user.LastLoginAt = null;
        user.CountryOfResidence = null;

        if (user.Profile is not null)
        {
            user.Profile.Biography = null;
            user.Profile.LinkedInUrl = null;
            user.Profile.WebsiteUrl = null;
            user.Profile.Nationality = null;
            user.Profile.DateOfBirth = null;
            user.Profile.CurrentInstitution = null;
            user.Profile.FieldOfStudy = null;
            user.Profile.Gpa = null;
            user.Profile.OrganizationLegalName = null;
            user.Profile.OrganizationRegistrationNumber = null;
            user.Profile.OrganizationWebsite = null;
            user.Profile.PreferredCountriesJson = null;
            user.Profile.PreferredFieldsJson = null;
            user.Profile.ExpertiseTagsJson = null;
            user.Profile.LanguagesJson = null;
        }

        // ── Education history (free-text institution / degree) ──────────────
        if (user.Profile is not null)
        {
            var profileId = user.Profile.Id;
            var education = await db.EducationEntries
                .IgnoreQueryFilters()
                .Where(e => e.UserProfileId == profileId)
                .ToListAsync(ct)
                .ConfigureAwait(false);
            foreach (var e in education)
            {
                e.InstitutionName = "[removed]";
                e.Degree = "[removed]";
                e.FieldOfStudy = "[removed]";
                e.Description = null;
            }
        }

        // ── Auth artefacts ──────────────────────────────────────────────────
        var tokens = await db.RefreshTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var t in tokens)
        {
            if (!t.IsRevoked)
            {
                t.IsRevoked = true;
                t.RevokedAt = now;
                t.RevokedReason = "account deleted";
            }
            t.IpAddress = null;
            t.UserAgent = null;
        }

        var resetTokens = await db.PasswordResetTokens
            .IgnoreQueryFilters()
            .Where(t => t.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        db.PasswordResetTokens.RemoveRange(resetTokens);

        // Login attempts hold email + IP + UA — personal data, not aggregate-essential.
        var loginAttempts = await db.LoginAttempts
            .IgnoreQueryFilters()
            .Where(l => l.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var l in loginAttempts)
        {
            l.Email = "[removed]";
            l.IpAddress = null;
            l.UserAgent = null;
        }

        // ── Chat — message bodies are free-text personal content ────────────
        var messages = await db.Messages
            .IgnoreQueryFilters()
            .Where(m => m.SenderId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var m in messages)
        {
            m.Body = "[message removed]";
            m.IsDeleted = true;
            m.DeletedAt = now;
            m.DeletedByUserId = null;
        }

        // ── Forum — post bodies / titles are free-text personal content ─────
        var posts = await db.ForumPosts
            .IgnoreQueryFilters()
            .Where(p => p.AuthorId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var p in posts)
        {
            p.Title = p.Title is null ? null : "[removed]";
            p.BodyMarkdown = "[content removed]";
            p.IsDeleted = true;
            p.DeletedAt = now;
            p.DeletedByUserId = null;
        }

        var flags = await db.ForumFlags
            .IgnoreQueryFilters()
            .Where(f => f.FlaggedByUserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var f in flags)
        {
            f.AdditionalDetails = null;
        }

        // ── AI interactions — prompt / response can contain anything ────────
        var ai = await db.AiInteractions
            .IgnoreQueryFilters()
            .Where(a => a.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var a in ai)
        {
            a.PromptText = "[removed]";
            a.ResponseText = "[removed]";
            a.MetadataJson = null;
        }

        var redactionSamples = await db.AiRedactionAuditSamples
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var s in redactionSamples)
        {
            s.RedactedPrompt = "[removed]";
        }

        // ── Reviews — free-text comments ────────────────────────────────────
        var companyReviews = await db.ScholarshipProviderReviews
            .IgnoreQueryFilters()
            .Where(r => r.StudentId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var r in companyReviews)
        {
            r.Comment = null;
        }

        var consultantReviews = await db.ConsultantReviews
            .IgnoreQueryFilters()
            .Where(r => r.StudentId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var r in consultantReviews)
        {
            r.Comment = null;
        }

        // ── Application trackers — personal notes / submitted form data ─────
        var applications = await db.Applications
            .IgnoreQueryFilters()
            .Where(a => a.StudentId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var a in applications)
        {
            a.PersonalNotes = null;
            a.FormDataJson = null;
            a.AttachedDocumentsJson = null;
        }

        // ── Saved-scholarship notes ─────────────────────────────────────────
        var savedScholarships = await db.SavedScholarships
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var s in savedScholarships)
        {
            s.Note = null;
        }

        // ── Success stories — author display name / image ──────────────────
        var stories = await db.SuccessStories
            .IgnoreQueryFilters()
            .Where(s => s.StudentId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var s in stories)
        {
            s.AuthorDisplayName = "Former member";
            s.AuthorImageUrl = null;
        }

        // ── Audit log — IP / UA tied to this actor are personal data.
        // We keep the row (accountability) but strip the network identifiers.
        var auditRows = await db.AuditLogs
            .Where(a => a.ActorUserId == userId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var a in auditRows)
        {
            a.IpAddress = null;
            a.UserAgent = null;
        }

        // Payments / payouts are intentionally retained for legal/financial
        // record-keeping. They carry no name/email — only the user GUID, which
        // now resolves to the anonymised user row above, so no PII remains.
    }
}
