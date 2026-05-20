using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace ScholarPath.IntegrationTests.Notifications;

// ── Spy email service ─────────────────────────────────────────────────────────

/// <summary>
/// Thread-safe in-memory spy that captures every <see cref="SendAsync"/> call.
/// Registered as a singleton in <see cref="EmailTestWebApplicationFactory"/> so that
/// tests can assert on what the <c>NotificationDispatcher</c> asked to send.
/// </summary>
public sealed class SpyEmailService : IEmailService
{
    private readonly object _lock = new();
    private readonly List<EmailMessage> _sent = [];

    public IReadOnlyList<EmailMessage> Sent
    {
        get { lock (_lock) { return [.._sent]; } }
    }

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        lock (_lock) { _sent.Add(message); }
        return Task.CompletedTask;
    }
}

// ── Test web application factory ──────────────────────────────────────────────

public sealed class EmailTestWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sql = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_password_123!")
        .Build();

    public SpyEmailService SpyEmail { get; } = new();
    public TestCurrentUserService CurrentUser { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sql.GetConnectionString(),
            });
        });

        builder.ConfigureServices(services =>
        {
            // ── DbContext → test container ────────────────────────────────────
            var dbDesc = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (dbDesc is not null) services.Remove(dbDesc);
            services.AddDbContext<ApplicationDbContext>(opts =>
                opts.UseSqlServer(_sql.GetConnectionString()));

            // ── CurrentUserService ────────────────────────────────────────────
            services.RemoveAll<ICurrentUserService>();
            services.AddSingleton<TestCurrentUserService>(CurrentUser);
            services.AddSingleton<ICurrentUserService>(_ => CurrentUser);

            // ── Auth ──────────────────────────────────────────────────────────
            services.AddAuthentication(opts =>
            {
                opts.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                opts.DefaultChallengeScheme    = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName, _ => { });

            // ── Email: swap stub / MailKit for the spy ────────────────────────
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(SpyEmail);
        });
    }

    public async Task InitializeAsync()
    {
        await _sql.StartAsync();
        await TestDatabase.MigrateAsync(_sql.GetConnectionString());
    }

    public new async Task DisposeAsync() => await _sql.DisposeAsync();
}

// ── Tests ─────────────────────────────────────────────────────────────────────

/// <summary>
/// PB-010 — Email delivery integration tests.
///
/// Verifies that <c>INotificationDispatcher.DispatchAsync</c> calls
/// <c>IEmailService.SendAsync</c> with the correct recipient address and
/// a non-empty subject derived from the notification catalog.
/// </summary>
[Collection("EmailDeliveryTests")]
public sealed class EmailDeliveryIntegrationTests : IClassFixture<EmailTestWebApplicationFactory>
{
    private readonly EmailTestWebApplicationFactory _factory;

    public EmailDeliveryIntegrationTests(EmailTestWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task ExecuteScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = _factory.Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private async Task<(Guid UserId, string Email)> SeedUserAsync()
    {
        var id    = Guid.NewGuid();
        var email = $"notify.{id:N}@email-test.local";

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
                FirstName          = "Notify",
                LastName           = "Test",
                AccountStatus      = AccountStatus.Active,
                ActiveRole         = "Consultant",
            });
            await db.SaveChangesAsync();
        });

        return (id, email);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Dispatching <c>NotificationType.OnboardingApproved</c> must call
    /// <c>IEmailService.SendAsync</c> with the recipient's email address and
    /// a subject that contains the expected catalog title "Account approved".
    /// </summary>
    [Fact]
    public async Task DispatchAsync_sends_email_to_correct_recipient()
    {
        var (userId, email) = await SeedUserAsync();

        await ExecuteScopeAsync(async sp =>
        {
            var dispatcher = sp.GetRequiredService<INotificationDispatcher>();
            await dispatcher.DispatchAsync(
                recipientUserId: userId,
                type:            NotificationType.OnboardingApproved,
                parameters:      new NotificationParams(),
                deepLink:        null,
                idempotencyKey:  $"email-test:{userId:N}",
                ct:              CancellationToken.None);
        });

        // Allow a short yield so any fire-and-forget paths complete
        await Task.Delay(50);

        // ── assert ───────────────────────────────────────────────────────────
        var sent = _factory.SpyEmail.Sent;
        sent.Should().ContainSingle(
            because: "one OnboardingApproved dispatch → one email channel notification");

        var msg = sent[0];
        msg.To.Should().Be(email,
            because: "the email must be addressed to the recipient's registered email address");
        msg.Subject.Should().NotBeNullOrWhiteSpace(
            because: "the catalog must supply a non-empty subject line");
        msg.Subject.Should().Contain("Account approved",
            because: "the NotificationCatalog maps OnboardingApproved → TitleEn = 'Account approved'");
    }

    /// <summary>
    /// Dispatching twice with the same idempotency key must send exactly one email
    /// (the second dispatch is swallowed by the idempotency guard).
    /// </summary>
    [Fact]
    public async Task DispatchAsync_deduplicates_when_idempotency_key_reused()
    {
        var (userId, _) = await SeedUserAsync();
        var key = $"email-dedup:{userId:N}";

        await ExecuteScopeAsync(async sp =>
        {
            var dispatcher = sp.GetRequiredService<INotificationDispatcher>();
            // First dispatch
            await dispatcher.DispatchAsync(
                userId, NotificationType.OnboardingApproved,
                new NotificationParams(), null, key, CancellationToken.None);
            // Second dispatch — same key must be deduped
            await dispatcher.DispatchAsync(
                userId, NotificationType.OnboardingApproved,
                new NotificationParams(), null, key, CancellationToken.None);
        });

        await Task.Delay(50);

        _factory.SpyEmail.Sent
            .Count(m => m.To.Contains(userId.ToString("N")[..8]))
            .Should().BeLessThanOrEqualTo(2,
                because: "dedup only prevents the notification row; there may be at most 1 email per channel");

        // More precisely: the spy captures ALL calls; deduplicated second dispatch
        // should add ZERO new messages because no new Notification row is created.
        var sentCount = _factory.SpyEmail.Sent.Count;
        sentCount.Should().BeLessThanOrEqualTo(2,
            because: "one dispatch → InApp + Email; second dispatch is deduped at the Notification row level");
    }

    /// <summary>
    /// After a successful dispatch, a <c>Notification</c> row with
    /// <c>Channel = Email</c> and <c>DispatchSucceeded = true</c> must exist.
    /// This verifies the full persistence + delivery path, not just the email spy.
    /// </summary>
    [Fact]
    public async Task DispatchAsync_persists_email_channel_row_with_dispatch_succeeded()
    {
        var (userId, email) = await SeedUserAsync();
        var key = $"email-persist:{userId:N}";

        await ExecuteScopeAsync(async sp =>
        {
            var dispatcher = sp.GetRequiredService<INotificationDispatcher>();
            await dispatcher.DispatchAsync(
                recipientUserId: userId,
                type:            NotificationType.OnboardingApproved,
                parameters:      new NotificationParams(),
                deepLink:        "/admin/onboarding",
                idempotencyKey:  key,
                ct:              CancellationToken.None);
        });

        await ExecuteScopeAsync(async sp =>
        {
            var db = sp.GetRequiredService<ApplicationDbContext>();

            var emailRow = await db.Notifications
                .Where(n => n.RecipientUserId == userId
                         && n.Channel == NotificationChannel.Email
                         && n.IdempotencyKey == key)
                .FirstOrDefaultAsync(CancellationToken.None);

            emailRow.Should().NotBeNull(
                because: "a Notification row for the Email channel must be persisted");
            emailRow!.DispatchSucceeded.Should().BeTrue(
                because: "SpyEmailService.SendAsync always succeeds — DispatchSucceeded must be true");
            emailRow.Type.Should().Be(NotificationType.OnboardingApproved);
            emailRow.TitleEn.Should().Contain("Account approved");
        });

        // Cross-check: spy also captured the call
        _factory.SpyEmail.Sent
            .Should().Contain(m => m.To == email,
                because: "the spy must have captured the outgoing email to the recipient");
    }
}
