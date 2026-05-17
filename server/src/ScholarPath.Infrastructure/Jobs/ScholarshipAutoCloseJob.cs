using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

/// <summary>
/// Recurring job (FR-230) that closes <see cref="ScholarshipStatus.Open"/> listings
/// once their deadline has passed. Closed listings stop accepting new applications
/// (<c>StartApplicationCommandHandler</c> requires status <c>Open</c>) without an
/// admin having to archive each one by hand.
/// </summary>
public sealed class ScholarshipAutoCloseJob(
    ApplicationDbContext db,
    ILogger<ScholarshipAutoCloseJob> logger) : IScholarshipAutoCloseJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Find Open listings whose deadline is in the past.
        var expired = await db.Scholarships
            .Where(s => s.Status == ScholarshipStatus.Open && s.Deadline < now)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (expired.Count == 0)
        {
            logger.LogInformation("[job] ScholarshipAutoCloseJob — no listings past deadline.");
            return;
        }

        foreach (var scholarship in expired)
        {
            scholarship.Status = ScholarshipStatus.Closed;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "[job] ScholarshipAutoCloseJob — closed {Count} scholarship(s) past deadline.",
            expired.Count);
    }
}
