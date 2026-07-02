using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface IDataExportJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Sweeps pending Export requests, collects user data into a JSON blob,
/// uploads it, and fills in DownloadUrl + CompletedAt.
/// Recurring daily.
///
/// GDPR Art. 15 (right of access): the export must contain ALL personal data
/// the platform holds about the data subject, across every related table.
/// </summary>
public sealed class DataExportJob(
    ApplicationDbContext db,
    IBlobStorageService blob,
    IEmailService email,
    IDateTimeService clock,
    ILogger<DataExportJob> logger) : IDataExportJob
{
    private static readonly TimeSpan DownloadTtl = TimeSpan.FromDays(14);

    public async Task RunAsync(CancellationToken ct)
    {
        var pending = await db.UserDataRequests
            .Where(r => r.Type == UserDataRequestType.Export
                && r.Status == UserDataRequestStatus.Pending)
            .OrderBy(r => r.RequestedAt)
            .Take(50)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pending.Count == 0) return;

        logger.LogInformation("[job] DataExport processing {Count} requests", pending.Count);

        foreach (var req in pending)
        {
            try
            {
                req.Status = UserDataRequestStatus.Processing;
                req.UpdatedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                var payload = await BuildExportPayloadAsync(req.UserId, ct).ConfigureAwait(false);
                var bytes = JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions { WriteIndented = true });

                using var ms = new MemoryStream(bytes);
                var url = await blob.UploadAsync(
                    ms,
                    $"export-{req.UserId}-{req.Id}.json",
                    "application/json",
                    container: "data-exports",
                    ct).ConfigureAwait(false);

                var now = clock.UtcNow;
                req.Status = UserDataRequestStatus.Completed;
                req.DownloadUrl = url;
                req.DownloadExpiresAt = now.Add(DownloadTtl);
                req.CompletedAt = now;
                req.UpdatedAt = now;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                // Audit the access event itself (GDPR accountability — Art. 5(2)).
                db.AuditLogs.Add(new Domain.Entities.AuditLog
                {
                    ActorUserId = null, // system job
                    Action = AuditAction.Update,
                    TargetType = "UserDataRequest",
                    TargetId = req.Id,
                    Summary = $"Data export completed for user {req.UserId}",
                    OccurredAt = now,
                });
                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                // notify the user by email (MailHog in dev)
                var user = await db.Users
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(u => u.Id == req.UserId, ct)
                    .ConfigureAwait(false);
                if (user?.Email is not null)
                {
                    await email.SendAsync(new EmailMessage(
                        user.Email,
                        "Your ScholarPath data export is ready",
                        HtmlBody: $"<p>Your data export is ready to download. The link expires on {req.DownloadExpiresAt:u}.</p><p><a href=\"{url}\">Download</a></p>",
                        TextBody: $"Your data export is ready: {url} (expires {req.DownloadExpiresAt:u})"
                    ), ct).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[job] DataExport failed for request {Id}", req.Id);
                req.Status = UserDataRequestStatus.Failed;
                req.FailureReason = ex.Message.Length > 2000 ? ex.Message[..2000] : ex.Message;
                req.UpdatedAt = clock.UtcNow;
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
        }
    }

    private async Task<object> BuildExportPayloadAsync(Guid userId, CancellationToken ct)
    {
        // IgnoreQueryFilters throughout: the export must include rows that have
        // been soft-deleted too — they are still personal data the platform holds.
        var user = await db.Users.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id, u.Email, u.UserName, u.PhoneNumber, u.FirstName, u.LastName,
                u.PreferredLanguage, u.CountryOfResidence, u.ProfileImageUrl,
                u.CreatedAt, u.LastLoginAt, u.AccountStatus, u.IsOnboardingComplete,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var profile = await db.UserProfiles.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var education = profile is null
            ? []
            : await db.EducationEntries.AsNoTracking()
                .IgnoreQueryFilters()
                .Where(e => e.UserProfileId == profile.Id)
                .Select(e => new { e.Id, e.InstitutionName, e.Degree, e.FieldOfStudy, e.StartDate, e.EndDate, e.Gpa, e.Description })
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var applications = await db.Applications.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.StudentId == userId)
            .Select(a => new
            {
                a.Id, a.ScholarshipId, a.Status, a.Mode, a.SubmittedAt, a.DecisionAt,
                a.WithdrawnAt, a.DecisionReason, a.PersonalNotes, a.FormDataJson, a.ExternalTrackingUrl,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var savedScholarships = await db.SavedScholarships.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(s => s.UserId == userId)
            .Select(s => new { s.Id, s.ScholarshipId, s.SavedAt, s.Note })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bookings = await db.Bookings.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.StudentId == userId || b.ConsultantId == userId)
            .Select(b => new { b.Id, b.StudentId, b.ConsultantId, b.Status, b.ScheduledStartAt, b.ScheduledEndAt, b.PriceUsd })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var availabilities = await db.Availabilities.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.ConsultantId == userId)
            .Select(a => new { a.Id, a.DayOfWeek, a.StartTime, a.EndTime, a.SpecificStartAt, a.SpecificEndAt, a.Timezone, a.IsRecurring })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var companyReviews = await db.ScholarshipProviderReviews.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.StudentId == userId)
            .Select(r => new { r.Id, r.ScholarshipProviderId, r.Rating, r.Comment, r.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var consultantReviews = await db.ConsultantReviews.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(r => r.StudentId == userId)
            .Select(r => new { r.Id, r.ConsultantId, r.Rating, r.Comment, r.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var forumPosts = await db.ForumPosts.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.AuthorId == userId)
            .Select(p => new { p.Id, p.CategoryId, p.ParentPostId, p.Title, p.BodyMarkdown, p.ModerationStatus, p.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var forumVotes = await db.ForumVotes.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(v => v.UserId == userId)
            .Select(v => new { v.Id, v.ForumPostId, v.VoteType, v.VotedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var forumFlags = await db.ForumFlags.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(f => f.FlaggedByUserId == userId)
            .Select(f => new { f.Id, f.ForumPostId, f.Reason, f.AdditionalDetails, f.FlaggedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var chatMessages = await db.Messages.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(m => m.SenderId == userId)
            .Select(m => new { m.Id, m.ConversationId, m.Body, m.SentAt, m.ReadAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var aiInteractions = await db.AiInteractions.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Id, a.Feature, a.PromptText, a.ResponseText, a.MetadataJson, a.StartedAt, a.CompletedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourceBookmarks = await db.ResourceBookmarks.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(b => b.UserId == userId)
            .Select(b => new { b.Id, b.ResourceId, b.BookmarkedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var resourceProgress = await db.ResourceProgress.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Id, p.ResourceId, p.ChaptersCompletedCount, p.LastAccessedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var upgradeRequests = await db.UpgradeRequests.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Id, u.Target, u.Status, u.Reason, u.CreatedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var payments = await db.Payments.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.PayerUserId == userId || p.PayeeUserId == userId)
            .Select(p => new { p.Id, p.Type, p.Status, p.AmountCents, p.Currency, p.CreatedAt, p.CapturedAt, p.RefundedAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var notifications = await db.Notifications.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(n => n.RecipientUserId == userId)
            .Select(n => new { n.Id, n.Type, n.TitleEn, n.BodyEn, n.CreatedAt, n.IsRead })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var notificationPreferences = await db.NotificationPreferences.AsNoTracking()
            .IgnoreQueryFilters()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.Id, p.Type, p.Channel, p.IsEnabled })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new
        {
            ExportedAt = clock.UtcNow,
            User = user,
            Profile = profile,
            Education = education,
            Applications = applications,
            SavedScholarships = savedScholarships,
            Bookings = bookings,
            Availabilities = availabilities,
            ScholarshipProviderReviews = companyReviews,
            ConsultantReviews = consultantReviews,
            ForumPosts = forumPosts,
            ForumVotes = forumVotes,
            ForumFlags = forumFlags,
            ChatMessages = chatMessages,
            AiInteractions = aiInteractions,
            ResourceBookmarks = resourceBookmarks,
            ResourceProgress = resourceProgress,
            UpgradeRequests = upgradeRequests,
            Payments = payments,
            Notifications = notifications,
            NotificationPreferences = notificationPreferences,
        };
    }
}
