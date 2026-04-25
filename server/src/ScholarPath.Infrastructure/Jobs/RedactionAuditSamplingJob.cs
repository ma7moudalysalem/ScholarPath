using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface IRedactionAuditSamplingJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Monthly sampler (PB-017 US-178 / FR-254..FR-256). Picks up to
/// <see cref="TargetSampleSize"/> random Chatbot prompts from the previous
/// calendar month, snapshots the already-redacted text into
/// <see cref="AiRedactionAuditSample"/>, and leaves <c>Verdict = null</c> for
/// an admin to review. The sampling is idempotent per (month, interaction):
/// re-runs add only missing samples instead of duplicating.
/// </summary>
public sealed class RedactionAuditSamplingJob(
    ApplicationDbContext db,
    IDateTimeService clock,
    ILogger<RedactionAuditSamplingJob> logger) : IRedactionAuditSamplingJob
{
    // Tunable in code; a config toggle would be overkill for a monthly batch.
    private const int TargetSampleSize = 50;

    public async Task RunAsync(CancellationToken ct)
    {
        var now = clock.UtcNow;

        // Sample the month that just ended so the audit always looks at a
        // finalised window — no races with mid-month interactions still arriving.
        var monthEnd = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var monthStart = monthEnd.AddMonths(-1);

        // Pull candidate ids only — cheap projection, then order randomly.
        // NEWID() is fine for Chatbot cardinality (low thousands/month at our scale).
        var candidateIds = await db.AiInteractions
            .AsNoTracking()
            .Where(i => i.Feature == AiFeature.Chatbot
                        && i.StartedAt >= monthStart
                        && i.StartedAt < monthEnd
                        && !string.IsNullOrEmpty(i.PromptText))
            .OrderBy(_ => EF.Functions.Random())
            .Select(i => i.Id)
            .Take(TargetSampleSize * 2) // pad for ids already sampled
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (candidateIds.Count == 0)
        {
            logger.LogInformation(
                "[job] RedactionAuditSampling found no Chatbot prompts for {MonthStart:yyyy-MM}",
                monthStart);
            return;
        }

        var alreadySampled = await db.AiRedactionAuditSamples
            .AsNoTracking()
            .Where(s => candidateIds.Contains(s.AiInteractionId))
            .Select(s => s.AiInteractionId)
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var alreadySet = alreadySampled.ToHashSet();

        var toSample = candidateIds
            .Where(id => !alreadySet.Contains(id))
            .Take(TargetSampleSize)
            .ToList();

        if (toSample.Count == 0)
        {
            logger.LogInformation(
                "[job] RedactionAuditSampling already satisfied for {MonthStart:yyyy-MM}",
                monthStart);
            return;
        }

        var rows = await db.AiInteractions
            .AsNoTracking()
            .Where(i => toSample.Contains(i.Id))
            .Select(i => new { i.Id, i.UserId, i.PromptText })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        foreach (var r in rows)
        {
            db.AiRedactionAuditSamples.Add(new AiRedactionAuditSample
            {
                AiInteractionId = r.Id,
                UserId = r.UserId,
                // PromptText is already redacted at ingestion — we snapshot it here
                // so later PII-policy improvements don't rewrite historical samples.
                RedactedPrompt = Truncate(r.PromptText, 2000),
                SampledAt = now,
                Verdict = null,
            });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "[job] RedactionAuditSampling queued {Count} samples for {MonthStart:yyyy-MM}",
            rows.Count, monthStart);
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max];
}
