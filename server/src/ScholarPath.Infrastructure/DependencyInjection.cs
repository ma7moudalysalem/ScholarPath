using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
    {
        // ─── Options ────────────────────────────────────────────────────────────
        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        services.Configure<StripeOptions>(config.GetSection(StripeOptions.SectionName));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.SectionName));
        services.Configure<RedisOptions>(config.GetSection(RedisOptions.SectionName));
        services.Configure<HangfireOptions>(config.GetSection(HangfireOptions.SectionName));
        services.Configure<StorageOptions>(config.GetSection(StorageOptions.SectionName));
        services.Configure<AiOptions>(config.GetSection(AiOptions.SectionName));
        services.Configure<AppOptions>(config.GetSection(AppOptions.SectionName));
        services.Configure<AuthenticationOptions>(config.GetSection(AuthenticationOptions.SectionName));
        services.Configure<FileScanningOptions>(config.GetSection(FileScanningOptions.SectionName));

        // Project AiOptions into the Application-side snapshot so the cost gate
        // doesn't have to know about Infrastructure's full options type.
        services.Configure<AiCostOptionsSnapshot>(snap =>
        {
            var ai = config.GetSection(AiOptions.SectionName).Get<AiOptions>() ?? new AiOptions();
            snap.DailyUserCostLimitUsd = ai.DailyUserCostLimitUsd;
            snap.RecommendationTopN = ai.RecommendationTopN;
        });

        // ─── Database ───────────────────────────────────────────────────────────
        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");

        services.AddDbContext<ApplicationDbContext>(opts =>
        {
            opts.UseSqlServer(connectionString, sql =>
            {
                sql.EnableRetryOnFailure(3);
                sql.CommandTimeout(30);
                sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
            opts.EnableSensitiveDataLogging(false);
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // ─── ASP.NET Core Identity ──────────────────────────────────────────────
        services.AddIdentity<ApplicationUser, ApplicationRole>(opts =>
        {
            opts.Password.RequireDigit = true;
            opts.Password.RequireUppercase = true;
            opts.Password.RequireLowercase = true;
            opts.Password.RequireNonAlphanumeric = true;
            opts.Password.RequiredLength = 8;
            opts.User.RequireUniqueEmail = true;
            opts.SignIn.RequireConfirmedEmail = false;
            opts.Lockout.MaxFailedAccessAttempts = 5;
            opts.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            opts.Lockout.AllowedForNewUsers = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // ─── Domain / Application services ──────────────────────────────────────
        services.AddHttpContextAccessor();
        services.AddSingleton<IDateTimeService, DateTimeService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Auth / Identity adapters
        services.AddScoped<ITokenService, TokenService>();
        services.AddSingleton<IPasswordHasher, StubPasswordHasher>();

        // SSO: the real Google/Microsoft OAuth implementation by default. It
        // becomes fully functional once real client credentials replace the
        // placeholders in the Authentication:* config. Set Authentication:UseStub
        // = true (dev / tests) to fall back to the deterministic StubSsoService.
        var useStubSso = config.GetValue<bool>($"{AuthenticationOptions.SectionName}:UseStub");
        if (useStubSso)
        {
            services.AddSingleton<ISsoService, StubSsoService>();
        }
        else
        {
            services.AddHttpClient(SsoService.HttpClientName, http =>
            {
                http.Timeout = TimeSpan.FromSeconds(15);
            });
            services.AddScoped<ISsoService, SsoService>();
        }

        // Email verification (FR-215) — wraps Identity's built-in confirmation tokens.
        services.AddScoped<IEmailVerificationService, EmailVerificationService>();

        // JWT signing key source (RS256). Key Vault when a vault URI is
        // configured (production); otherwise the local PEM / ephemeral-key
        // provider for development. Registered as a single shared instance so
        // the token signer (private key) and the JWT-bearer validator (public
        // key) use the SAME RSA key — critical for the dev provider, whose
        // ephemeral key must not differ between the two. The instance is built
        // here and also handed to AddJwtBearer in Program.cs.
        services.AddSingleton(JwtKeyProviderRegistration.GetOrCreate(config));

        // Email: real SMTP via MailKit when Email:Provider = "MailKit"; else the dev stub.
        var emailProvider = config.GetValue<string>($"{EmailOptions.SectionName}:Provider");
        if (string.Equals(emailProvider, "MailKit", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IEmailService, MailKitEmailService>();
        else
            services.AddSingleton<IEmailService, StubEmailService>();

        // File storage: real provider driven by Storage:Provider (Local | AzureBlob).
        // The document vault (FR-216) needs working upload/download/delete, which the
        // dev stub cannot give. FileStorageService honours both providers.
        services.AddSingleton<IBlobStorageService, FileStorageService>();

        // Antivirus scanning (security NFR): the real ClamAV scanner only when
        // FileScanning:Enabled is true and a reachable clamd daemon is
        // configured; otherwise the NoOpFileScanService keeps dev / tests
        // working without a daemon. Every upload path scans bytes through
        // IFileScanService before storing them and rejects a non-clean verdict.
        var fileScanningEnabled = config.GetValue<bool>($"{FileScanningOptions.SectionName}:Enabled");
        if (fileScanningEnabled)
            services.AddSingleton<IFileScanService, ClamAvFileScanService>();
        else
            services.AddSingleton<IFileScanService, NoOpFileScanService>();

        // Stripe: the real client only when a genuine secret key (sk_...) is
        // configured; placeholder values fall through to the dev stub.
        var stripeSecretKey = config.GetValue<string>($"{StripeOptions.SectionName}:SecretKey");
        if (stripeSecretKey is not null && stripeSecretKey.StartsWith("sk_", StringComparison.Ordinal))
            services.AddSingleton<IStripeService, StripeService>();
        else
            services.AddSingleton<IStripeService, StubStripeService>();

        // AI provider selection: Local (default, deterministic, offline) or
        // OpenAi (real provider, needs Ai:OpenAi:ApiKey). Swap via config —
        // no code changes. OpenAI service itself falls back to Local on
        // network failure so the UX degrades gracefully.
        var aiProvider = config.GetValue<string>($"{AiOptions.SectionName}:Provider") ?? "Stub";
        if (string.Equals(aiProvider, "OpenAi", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient("openai");
            services.AddScoped<IAiService, OpenAiService>();
        }
        else
        {
            services.AddScoped<IAiService, LocalAiService>();
        }

        // Notifications: catalog renders the bilingual text; the real dispatcher
        // persists a Notification row per channel and delivers InApp + Email (Task 5B).
        services.AddSingleton<INotificationCatalog, NotificationCatalog>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();
        services.AddScoped<ICommunityRealtimeNotifier, CommunityRealtimeNotifier>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserAdministration, UserAdministration>();
        services.AddScoped<IEmailChangeService, EmailChangeService>();
        services.AddScoped<IAdminReadService, AdminReadService>();
        services.AddScoped<AiCostGate>();

        // Jobs
        services.AddScoped<IDeadlineReminderJob, DeadlineReminderJob>();
        services.AddScoped<INotificationDispatcherJob, NotificationDispatcherJob>();
        services.AddScoped<IStripePayoutJob, StripePayoutJob>();
        services.AddScoped<ISessionExpiryJob, SessionExpiryJob>();
        services.AddScoped<ICompletionJob, CompletionJob>();
        services.AddScoped<IIntegrityCheckJob, IntegrityCheckJob>();
        services.AddScoped<IDataExportJob, DataExportJob>();
        services.AddScoped<IDataDeleteJob, DataDeleteJob>();
        services.AddScoped<IRedactionAuditSamplingJob, RedactionAuditSamplingJob>();
        services.AddScoped<ICompanyReviewTimeoutRefundJob, CompanyReviewTimeoutRefundJob>();
        services.AddScoped<IScholarshipAutoCloseJob, ScholarshipAutoCloseJob>();

        // Memory cache (Redis swap handled at deploy time)
        services.AddMemoryCache();

        return services;
    }
}
