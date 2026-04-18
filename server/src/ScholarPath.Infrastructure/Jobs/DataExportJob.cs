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

                // notify the user by email (MailHog in dev)
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == req.UserId, ct).ConfigureAwait(false);
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
        var user = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id, u.Email, u.FirstName, u.LastName, u.PreferredLanguage,
                u.CountryOfResidence, u.CreatedAt, u.LastLoginAt, u.AccountStatus,
            })
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var profile = await db.UserProfiles.AsNoTracking()
            .Where(p => p.UserId == userId)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        var applications = await db.Applications.AsNoTracking()
            .Where(a => a.StudentId == userId)
            .Select(a => new { a.Id, a.ScholarshipId, a.Status, a.Mode, a.SubmittedAt, a.DecisionAt })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var bookings = await db.Bookings.AsNoTracking()
            .Where(b => b.StudentId == userId || b.ConsultantId == userId)
            .Select(b => new { b.Id, b.StudentId, b.ConsultantId, b.Status, b.ScheduledStartAt, b.PriceUsd })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == userId)
            .Select(n => new { n.Id, n.Type, n.TitleEn, n.CreatedAt, n.IsRead })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new
        {
            ExportedAt = clock.UtcNow,
            User = user,
            Profile = profile,
            Applications = applications,
            Bookings = bookings,
            Notifications = notifications,
        };
    }
}
