using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.UnitTests.Ai;

/// <summary>
/// Cost-gate behavior with a real (InMemory) DbContext so we exercise the
/// actual EF query path. Seeds a few historical AiInteraction rows and
/// flips the 24h rolling sum against the configured daily cap.
/// </summary>
public class AiCostGateTests
{
    private static (ApplicationDbContext db, IDateTimeService clock, AiCostGate gate) BuildSut(decimal capUsd)
    {
        var opts = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var db = new ApplicationDbContext(opts);

        var clock = Substitute.For<IDateTimeService>();
        clock.UtcNow.Returns(new DateTimeOffset(2026, 5, 1, 12, 0, 0, TimeSpan.Zero));

        var snapshot = Options.Create(new AiCostOptionsSnapshot { DailyUserCostLimitUsd = capUsd });
        var gate = new AiCostGate((IApplicationDbContext)db, clock, snapshot);

        return (db, clock, gate);
    }

    private static async Task SeedAsync(ApplicationDbContext db, Guid userId, DateTimeOffset startedAt, decimal costUsd)
    {
        db.AiInteractions.Add(new AiInteraction
        {
            UserId = userId,
            Feature = AiFeature.Chatbot,
            Provider = AiProvider.Stub,
            StartedAt = startedAt,
            PromptText = "x",
            ResponseText = "y",
            CostUsd = costUsd,
            CreatedAt = startedAt,
        });
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Allows_call_when_under_budget()
    {
        var (db, _, gate) = BuildSut(capUsd: 1.00m);
        var userId = Guid.NewGuid();
        await SeedAsync(db, userId, DateTimeOffset.UtcNow.AddHours(-1), 0.30m);

        var act = () => gate.EnsureWithinDailyBudgetAsync(userId, 0.20m, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Throws_Conflict_when_next_call_would_exceed_cap()
    {
        var (db, clock, gate) = BuildSut(capUsd: 1.00m);
        var userId = Guid.NewGuid();
        // 0.95 already used within the 24h window
        await SeedAsync(db, userId, clock.UtcNow.AddHours(-2), 0.95m);

        var act = () => gate.EnsureWithinDailyBudgetAsync(userId, 0.10m, CancellationToken.None);
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Daily AI budget exceeded*");
    }

    [Fact]
    public async Task Ignores_spend_older_than_24h()
    {
        var (db, clock, gate) = BuildSut(capUsd: 1.00m);
        var userId = Guid.NewGuid();
        // 0.99 spent but 25h ago — outside the rolling window
        await SeedAsync(db, userId, clock.UtcNow.AddHours(-25), 0.99m);

        var act = () => gate.EnsureWithinDailyBudgetAsync(userId, 0.50m, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Zero_cap_disables_the_gate()
    {
        var (db, _, gate) = BuildSut(capUsd: 0m);
        var userId = Guid.NewGuid();
        await SeedAsync(db, userId, DateTimeOffset.UtcNow, 99_999m);

        var act = () => gate.EnsureWithinDailyBudgetAsync(userId, 99_999m, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Each_user_is_tracked_independently()
    {
        var (db, clock, gate) = BuildSut(capUsd: 1.00m);
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await SeedAsync(db, user1, clock.UtcNow.AddMinutes(-30), 0.99m);

        // user1 is near the cap
        await Assert.ThrowsAsync<ConflictException>(() =>
            gate.EnsureWithinDailyBudgetAsync(user1, 0.10m, CancellationToken.None));

        // user2 has its own fresh budget
        var act = () => gate.EnsureWithinDailyBudgetAsync(user2, 0.99m, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
