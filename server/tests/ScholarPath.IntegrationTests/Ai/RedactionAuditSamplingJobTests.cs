using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.IntegrationTests.Ai;

/// <summary>
/// PB-017 T-015 — Sampling-job exactness integration tests.
///
/// Verifies three invariants against a real SQL Server container so that
/// EF.Functions.Random() (NEWID() in T-SQL) is exercised:
///
///   1. The job caps output at 50 samples even when the pool exceeds 50.
///   2. A re-run for the same month does not create duplicate samples.
///   3. Only interactions from the previous calendar month are sampled —
///      interactions from earlier months are ignored.
/// </summary>
public sealed class RedactionAuditSamplingJobTests : IntegrationTestBase
{
    // Fixed "now" so month boundaries are deterministic across every test run.
    // monthStart = 2026-04-01 00:00 UTC
    // monthEnd   = 2026-05-01 00:00 UTC
    private static readonly DateTimeOffset FakeNow =
        new(2026, 5, 20, 0, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset InApril(int day) =>
        new(2026, 4, day, 12, 0, 0, TimeSpan.Zero);

    private static DateTimeOffset InMarch(int day) =>
        new(2026, 3, day, 12, 0, 0, TimeSpan.Zero);

    public RedactionAuditSamplingJobTests(CustomWebApplicationFactory factory)
        : base(factory) { }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<Guid> SeedUserAsync()
    {
        var id = Guid.NewGuid();
        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id                 = id,
                UserName           = $"ai.test.{id:N}@test.local",
                NormalizedUserName = $"ai.test.{id:N}@test.local".ToUpperInvariant(),
                Email              = $"ai.test.{id:N}@test.local",
                NormalizedEmail    = $"ai.test.{id:N}@test.local".ToUpperInvariant(),
                EmailConfirmed     = true,
                FirstName          = "AI",
                LastName           = "Test",
                AccountStatus      = AccountStatus.Active,
                ActiveRole         = "Student",
            });
            await db.SaveChangesAsync();
        });
        return id;
    }

    private async Task SeedInteractionsAsync(
        Guid userId,
        int count,
        DateTimeOffset startedAt)
    {
        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            for (int i = 0; i < count; i++)
            {
                db.AiInteractions.Add(new AiInteraction
                {
                    UserId        = userId,
                    Feature       = AiFeature.Chatbot,
                    Provider      = AiProvider.Stub,
                    StartedAt     = startedAt,
                    PromptText    = $"test prompt {i} for {userId:N}",
                    ResponseText  = "response",
                    CostUsd       = 0.001m,
                    CreatedAt     = startedAt,
                });
            }
            await db.SaveChangesAsync();
        });
    }

    private static RedactionAuditSamplingJob BuildJob(ApplicationDbContext db)
    {
        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(FakeNow);
        return new RedactionAuditSamplingJob(
            db, clock, NullLogger<RedactionAuditSamplingJob>.Instance);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// When the pool of eligible interactions exceeds the 50-row cap, the job
    /// must produce exactly 50 samples — no more, no fewer.
    /// </summary>
    [Fact]
    public async Task RunAsync_caps_at_50_when_pool_exceeds_cap()
    {
        var userId = await SeedUserAsync();
        await SeedInteractionsAsync(userId, 60, InApril(5));

        await ExecuteScopeAsync(async sp =>
        {
            var db  = sp.GetRequiredService<ApplicationDbContext>();
            var job = BuildJob(db);
            await job.RunAsync(CancellationToken.None);
        });

        await ExecuteScopeAsync(async sp =>
        {
            var db    = sp.GetRequiredService<ApplicationDbContext>();
            var count = await db.AiRedactionAuditSamples
                .CountAsync(s => s.UserId == userId);
            count.Should().Be(50,
                because: "the job cap is 50 rows and 60 eligible interactions were seeded");
        });
    }

    /// <summary>
    /// Running the job a second time for the same month must not create duplicate
    /// samples. When the entire pool is already sampled the job exits early with
    /// no new rows.
    /// </summary>
    [Fact]
    public async Task RunAsync_is_idempotent_for_fully_sampled_month()
    {
        // Seed exactly 50 interactions — first run exhausts the pool.
        var userId = await SeedUserAsync();
        await SeedInteractionsAsync(userId, 50, InApril(10));

        await ExecuteScopeAsync(async sp =>
        {
            var db  = sp.GetRequiredService<ApplicationDbContext>();
            var job = BuildJob(db);
            await job.RunAsync(CancellationToken.None);  // first run → 50 samples
            await job.RunAsync(CancellationToken.None);  // second run → pool exhausted
        });

        await ExecuteScopeAsync(async sp =>
        {
            var db    = sp.GetRequiredService<ApplicationDbContext>();
            var count = await db.AiRedactionAuditSamples
                .CountAsync(s => s.UserId == userId);
            count.Should().Be(50,
                because: "re-running with all interactions already sampled must not create duplicates");
        });
    }

    /// <summary>
    /// The job samples only the PREVIOUS calendar month. Interactions from
    /// earlier months must never appear in the output.
    /// </summary>
    [Fact]
    public async Task RunAsync_only_samples_previous_month_interactions()
    {
        // 20 Chatbot interactions in April 2026 (target month)
        // 20 Chatbot interactions in March 2026 (should be ignored)
        var userId = await SeedUserAsync();
        await SeedInteractionsAsync(userId, 20, InApril(1));
        await SeedInteractionsAsync(userId, 20, InMarch(15));

        await ExecuteScopeAsync(async sp =>
        {
            var db  = sp.GetRequiredService<ApplicationDbContext>();
            var job = BuildJob(db);
            await job.RunAsync(CancellationToken.None);
        });

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();

            // Collect the StartedAt values of every sampled interaction for this user.
            var sampledIds = await db.AiRedactionAuditSamples
                .Where(s => s.UserId == userId)
                .Select(s => s.AiInteractionId)
                .ToListAsync();

            var startedAts = await db.AiInteractions
                .Where(i => sampledIds.Contains(i.Id))
                .Select(i => i.StartedAt)
                .ToListAsync();

            startedAts.Should().HaveCount(20,
                because: "all 20 April interactions are below the cap and the only eligible pool");
            startedAts.Should().AllSatisfy(d =>
                d.Month.Should().Be(4,
                    because: "only April (the previous calendar month) must be sampled, not March"));
        });
    }
}
