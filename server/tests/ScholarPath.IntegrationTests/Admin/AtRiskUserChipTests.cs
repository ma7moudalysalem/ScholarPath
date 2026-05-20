using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.IntegrationTests.Admin;

/// <summary>
/// PB-018 T-015 — Reverse-ETL at-risk chip integration tests.
///
/// Verifies that when a <see cref="UserRiskFlag"/> row is present for a user,
/// <c>IAdminReadService.SearchUsersAsync</c> correctly surfaces
/// <c>IsAtRisk = true</c> and the numeric <c>RiskScore</c> in the resulting
/// <see cref="ScholarPath.Application.Admin.DTOs.AdminUserRow"/>, and that the
/// absence of a flag row maps to <c>IsAtRisk = false, RiskScore = null</c>.
/// </summary>
public sealed class AtRiskUserChipTests : IntegrationTestBase
{
    public AtRiskUserChipTests(CustomWebApplicationFactory factory) : base(factory) { }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid UserId, string Email)> SeedUserAsync(string prefix = "student")
    {
        var id = Guid.NewGuid();
        var email = $"{prefix}.{id:N}@at-risk-test.local";

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.Users.Add(new ApplicationUser
            {
                Id                 = id,
                UserName           = email,
                NormalizedUserName = email.ToUpperInvariant(),
                Email              = email,
                NormalizedEmail    = email.ToUpperInvariant(),
                EmailConfirmed     = true,
                FirstName          = "AtRisk",
                LastName           = "Test",
                AccountStatus      = AccountStatus.Active,
                ActiveRole         = "Student",
            });
            await db.SaveChangesAsync();
        });

        return (id, email);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// When a <see cref="UserRiskFlag"/> row with <c>IsAtRisk = true</c> exists,
    /// the admin search result must expose <c>IsAtRisk = true</c> and the exact
    /// numeric score from the flag row.
    /// </summary>
    [Fact]
    public async Task SearchUsers_returns_isAtRisk_true_when_risk_flag_row_exists()
    {
        // ── arrange ──────────────────────────────────────────────────────────
        var (userId, email) = await SeedUserAsync("atrisk");
        const decimal riskScore = 0.82m;

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.UserRiskFlags.Add(new UserRiskFlag
            {
                Id             = Guid.NewGuid(),
                UserId         = userId,
                Score          = riskScore,
                IsAtRisk       = true,
                Reason         = "no activity for 60 days — integration test seed",
                ComputedAt     = DateTimeOffset.UtcNow,
                SourceRefreshId = Guid.NewGuid(),
            });
            await db.SaveChangesAsync();
        });

        // ── act ──────────────────────────────────────────────────────────────
        var row = await ExecuteScopeAsync(async sp =>
        {
            var admin = sp.GetRequiredService<IAdminReadService>();
            var result = await admin.SearchUsersAsync(
                search: email,
                status: null,
                role: null,
                includeDeleted: false,
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);
            return result.Items.FirstOrDefault(r => r.Id == userId);
        });

        // ── assert ───────────────────────────────────────────────────────────
        row.Should().NotBeNull(
            because: "the seeded user must appear in the admin search results");
        row!.IsAtRisk.Should().BeTrue(
            because: "a UserRiskFlag row with IsAtRisk=true was seeded for this user");
        row.RiskScore.Should().Be(riskScore,
            because: "the numeric score must propagate unchanged from UserRiskFlag.Score to AdminUserRow.RiskScore");
    }

    /// <summary>
    /// When NO <see cref="UserRiskFlag"/> row exists for a user, the search
    /// result must default to <c>IsAtRisk = false</c> and <c>RiskScore = null</c>
    /// (distinguishable from a score of zero).
    /// </summary>
    [Fact]
    public async Task SearchUsers_returns_isAtRisk_false_when_no_risk_flag_row()
    {
        // ── arrange ──────────────────────────────────────────────────────────
        var (userId, email) = await SeedUserAsync("norisk");
        // Deliberately no UserRiskFlag row for this user.

        // ── act ──────────────────────────────────────────────────────────────
        var row = await ExecuteScopeAsync(async sp =>
        {
            var admin = sp.GetRequiredService<IAdminReadService>();
            var result = await admin.SearchUsersAsync(
                search: email,
                status: null,
                role: null,
                includeDeleted: false,
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);
            return result.Items.FirstOrDefault(r => r.Id == userId);
        });

        // ── assert ───────────────────────────────────────────────────────────
        row.Should().NotBeNull(
            because: "the seeded user must appear in the admin search results");
        row!.IsAtRisk.Should().BeFalse(
            because: "no UserRiskFlag row was seeded — the missing row must map to IsAtRisk=false");
        row.RiskScore.Should().BeNull(
            because: "no UserRiskFlag row was seeded — RiskScore must be null, not zero");
    }

    /// <summary>
    /// A flag row with <c>IsAtRisk = false</c> (score computed but below threshold)
    /// must be reflected correctly in the search result.
    /// </summary>
    [Fact]
    public async Task SearchUsers_returns_isAtRisk_false_when_flag_row_says_false()
    {
        // ── arrange ──────────────────────────────────────────────────────────
        var (userId, email) = await SeedUserAsync("lowrisk");
        const decimal lowScore = 0.40m;

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();
            db.UserRiskFlags.Add(new UserRiskFlag
            {
                Id         = Guid.NewGuid(),
                UserId     = userId,
                Score      = lowScore,
                IsAtRisk   = false, // below chip threshold
                ComputedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        });

        // ── act ──────────────────────────────────────────────────────────────
        var row = await ExecuteScopeAsync(async sp =>
        {
            var admin = sp.GetRequiredService<IAdminReadService>();
            var result = await admin.SearchUsersAsync(
                search: email,
                status: null,
                role: null,
                includeDeleted: false,
                page: 1,
                pageSize: 10,
                ct: CancellationToken.None);
            return result.Items.FirstOrDefault(r => r.Id == userId);
        });

        // ── assert ───────────────────────────────────────────────────────────
        row.Should().NotBeNull();
        row!.IsAtRisk.Should().BeFalse(
            because: "the UserRiskFlag row has IsAtRisk=false (score below chip threshold)");
        row.RiskScore.Should().Be(lowScore,
            because: "the score must still propagate even when IsAtRisk is false");
    }
}
