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
/// Soft-deletes the user + anonymises aggregate-relevant rows.
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

                var user = await db.Users
                    .Include(u => u.Profile)
                    .FirstOrDefaultAsync(u => u.Id == req.UserId, ct)
                    .ConfigureAwait(false);

                if (user is not null)
                {
                    // Soft-delete + anonymize. We keep the row so foreign keys stay valid
                    // for aggregate reports, payouts, audit trail.
                    user.IsDeleted = true;
                    user.DeletedAt = now;
                    user.DeletedByUserId = user.Id;
                    user.Email = $"deleted+{user.Id}@scholarpath.invalid";
                    user.UserName = user.Email;
                    user.NormalizedEmail = user.Email.ToUpperInvariant();
                    user.NormalizedUserName = user.NormalizedEmail;
                    user.FirstName = "Deleted";
                    user.LastName = "User";
                    user.PhoneNumber = null;
                    user.ProfileImageUrl = null;

                    if (user.Profile is not null)
                    {
                        user.Profile.Biography = null;
                        user.Profile.LinkedInUrl = null;
                        user.Profile.WebsiteUrl = null;
                        user.Profile.Nationality = null;
                    }

                    // revoke any outstanding refresh tokens
                    var tokens = await db.RefreshTokens
                        .Where(t => t.UserId == user.Id && !t.IsRevoked)
                        .ToListAsync(ct)
                        .ConfigureAwait(false);
                    foreach (var t in tokens)
                    {
                        t.IsRevoked = true;
                        t.RevokedAt = now;
                        t.RevokedReason = "account deleted";
                    }
                }

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
}
