using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Ai.Common;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Hubs;
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
        services.Configure<FieldEncryptionOptions>(config.GetSection(FieldEncryptionOptions.SectionName));
        services.Configure<BookingOptions>(config.GetSection(BookingOptions.SectionName));
        services.Configure<EventHubOptions>(config.GetSection(EventHubOptions.SectionName));

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
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();

        // SSO: the real Google/Microsoft OAuth implementation by default. It
        // becomes fully functional once real client credentials replace the
        // placeholders in the Authentication:* config. Set Authentication:UseStub
        // = true (dev / tests) to fall back to the deterministic StubSsoService.
        //
        // AUTH-CODE-05 — if UseStub=false but provider credentials are clearly
        // missing/placeholder, surface a clear startup warning and fall back to
        // the stub so a misconfigured environment does not block boot or crash
        // on the first SSO click. The warning is emitted by Program.cs via the
        // ILogger pipeline, which is not yet built at this point — we stash the
        // status in a transient singleton it reads on startup.
        var authSection = config.GetSection(AuthenticationOptions.SectionName);
        var useStubSso = authSection.GetValue<bool>("UseStub");
        var ssoStatus = new SsoConfigurationStatus { UseStubRequested = useStubSso };

        if (!useStubSso)
        {
            var googleConfigured = IsProviderConfigured(authSection.GetSection("Google"));
            var microsoftConfigured = IsProviderConfigured(authSection.GetSection("Microsoft"));
            if (!googleConfigured || !microsoftConfigured)
            {
                ssoStatus.FellBackToStub = true;
                ssoStatus.MissingGoogle = !googleConfigured;
                ssoStatus.MissingMicrosoft = !microsoftConfigured;
                useStubSso = true;
            }
        }

        services.AddSingleton(ssoStatus);

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

        // Application-level field encryption (security NFR). Azure SQL TDE already
        // encrypts the whole database file at rest; this is the second, stronger
        // layer — specific sensitive columns are AES-256-GCM encrypted by the API
        // so they stay ciphertext even to direct DB query access. The AES key
        // comes from Key Vault when FieldEncryption:KeyVaultUri is set
        // (production); otherwise the local provider reads a fixed Base64 dev key.
        // Registered as a single shared instance so every consumer encrypts and
        // decrypts with the same key bytes. AesGcmFieldEncryptionService is a
        // singleton too — it holds only the immutable key — and is consumed by
        // the EF Core EncryptedStringConverter wired up in ApplicationDbContext.
        services.AddSingleton(FieldEncryptionKeyProviderRegistration.GetOrCreate(config));
        services.AddSingleton<IFieldEncryptionService, AesGcmFieldEncryptionService>();

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

        // Azure Event Hubs: real publisher when a connection string is provided;
        // otherwise the stub just logs events and continues without throwing.
        var ehConnString = config.GetValue<string>($"{EventHubOptions.SectionName}:ConnectionString");
        if (!string.IsNullOrWhiteSpace(ehConnString))
            services.AddSingleton<IEventPublisher, EventHubPublisher>();
        else
            services.AddSingleton<IEventPublisher, StubEventPublisher>();

        // Stripe: the real client only when a genuine secret key (sk_...) is
        // configured; placeholder values fall through to the dev stub.
        var stripeSecretKey = config.GetValue<string>($"{StripeOptions.SectionName}:SecretKey");
        if (stripeSecretKey is not null && stripeSecretKey.StartsWith("sk_", StringComparison.Ordinal))
            services.AddSingleton<IStripeService, StripeService>();
        else
            services.AddSingleton<IStripeService, StubStripeService>();

        // ── Video meeting provider ──
        // Azure Communication Services when an ACS connection string is
        // configured; otherwise the deterministic StubMeetingService keeps the
        // booking + no-show flows working offline — the same config-driven
        // fallback pattern as the Stripe and SSO providers above.
        var acsConnString = config.GetValue<string>($"{AcsOptions.SectionName}:ConnectionString");
        if (!string.IsNullOrWhiteSpace(acsConnString)
            && acsConnString.Contains("accesskey=", StringComparison.OrdinalIgnoreCase))
        {
            services.Configure<AcsOptions>(config.GetSection(AcsOptions.SectionName));
            services.AddSingleton<IMeetingService, AzureCommunicationMeetingService>();
        }
        else
        {
            services.AddSingleton<IMeetingService, StubMeetingService>();
        }

        // ── Power BI embedded analytics ──
        // Real PowerBiService when workspace + service principal are configured;
        // otherwise the no-op stub returns IsConfigured=false so the frontend
        // shows a "not yet configured" placeholder without throwing.
        var pbiWorkspace = config.GetValue<string>($"{PowerBiOptions.SectionName}:WorkspaceId");
        if (!string.IsNullOrWhiteSpace(pbiWorkspace))
        {
            services.Configure<PowerBiOptions>(config.GetSection(PowerBiOptions.SectionName));
            services.AddHttpClient("PowerBi");
            services.AddSingleton<IPowerBiService, PowerBiService>();
        }
        else
        {
            services.AddSingleton<IPowerBiService, StubPowerBiService>();
        }

        // ── AI provider + RAG pipeline ──
        // Provider is config-driven (no code change):
        //   Stub / Local → deterministic offline scoring + local-hash embeddings
        //   OpenAi       → OpenAI chat (needs Ai:OpenAi:ApiKey)
        //   AzureOpenAi  → Azure OpenAI chat + embeddings (needs Ai:AzureOpenAi:*)
        // Every provider grounds the chatbot in the RAG knowledge base, and the
        // cloud providers degrade to the local path on a network failure.
        var aiProvider = config.GetValue<string>($"{AiOptions.SectionName}:Provider") ?? "Stub";
        var useAzureAi = string.Equals(aiProvider, "AzureOpenAi", StringComparison.OrdinalIgnoreCase);
        var useOpenAi = string.Equals(aiProvider, "OpenAi", StringComparison.OrdinalIgnoreCase);

        // Embeddings: Azure when selected, otherwise the deterministic offline
        // local-hash embedder (no API key, no cost). LocalEmbeddingService is
        // always registered as a concrete service so the Azure provider can
        // delegate to it when the Azure embedding deployment isn't reachable.
        services.AddScoped<LocalEmbeddingService>();
        if (useAzureAi)
        {
            services.AddHttpClient("azure-openai");
            services.AddScoped<IEmbeddingService, AzureOpenAiEmbeddingService>();
        }
        else if (useOpenAi)
        {
            // OpenAI-direct embeddings with the same key as chat — without this an
            // OpenAi-configured deployment produced REAL chat but FAKE (local-hash)
            // retrieval vectors.
            services.AddHttpClient("openai");
            services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        }
        else
        {
            services.AddScoped<IEmbeddingService>(sp => sp.GetRequiredService<LocalEmbeddingService>());
        }

        // RAG knowledge base — bundled datasets, retriever, and indexer.
        services.AddSingleton<IDatasetProvider, EmbeddedDatasetProvider>();
        services.AddScoped<IKnowledgeRetriever, KnowledgeRetriever>();
        services.AddScoped<IKnowledgeBaseIndexer, KnowledgeBaseIndexer>();

        // LocalAiService is always registered as a concrete service so the cloud
        // providers can delegate scoring to it and fall back to it.
        services.AddScoped<LocalAiService>();
        if (useAzureAi)
        {
            services.AddScoped<IAiService, AzureOpenAiService>();
        }
        else if (useOpenAi)
        {
            services.AddHttpClient("openai");
            services.AddScoped<IAiService, OpenAiService>();
        }
        else
        {
            services.AddScoped<IAiService>(sp => sp.GetRequiredService<LocalAiService>());
        }

        // Notifications: catalog renders the bilingual text; the real dispatcher
        // persists a Notification row per channel and delivers InApp + Email (Task 5B).
        services.AddSingleton<INotificationCatalog, NotificationCatalog>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<IChatRealtimeNotifier, ChatRealtimeNotifier>();
        services.AddScoped<ICommunityRealtimeNotifier, CommunityRealtimeNotifier>();

        // Chat presence — in-memory, ref-counted SignalR connection tracking,
        // shared by every ChatHub instance, so the UI can show live online dots.
        // Resolves under two interfaces backed by the same singleton: the hub
        // mutates state through IPresenceTracker; Application handlers read it
        // through IChatPresenceQuery to suppress noisy email notifications when
        // the recipient already has the chat page open.
        services.AddSingleton<PresenceTracker>();
        services.AddSingleton<IPresenceTracker>(sp => sp.GetRequiredService<PresenceTracker>());
        services.AddSingleton<IChatPresenceQuery>(sp => sp.GetRequiredService<PresenceTracker>());
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserAdministration, UserAdministration>();
        services.AddScoped<IEmailChangeService, EmailChangeService>();
        services.AddScoped<IAdminReadService, AdminReadService>();
        services.AddScoped<IConsultantReadService, ConsultantReadService>();
        services.AddScoped<IChatContactReadService, ChatContactReadService>();
        services.AddScoped<AiCostGate>();

        // Jobs
        services.AddScoped<IDeadlineReminderJob, DeadlineReminderJob>();
        services.AddScoped<INotificationDispatcherJob, NotificationDispatcherJob>();
        services.AddScoped<IStripePayoutJob, StripePayoutJob>();
        services.AddScoped<ISessionExpiryJob, SessionExpiryJob>();
        services.AddScoped<ICompletionJob, CompletionJob>();
        services.AddScoped<IBookingReminderJob, BookingReminderJob>();
        services.AddScoped<IMeetingNoShowSweepJob, MeetingNoShowSweepJob>();
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

    /// <summary>
    /// AUTH-CODE-05 — a provider section is considered "configured" only when
    /// both client id and client secret are non-empty and not the
    /// PLACEHOLDER_… defaults shipped in appsettings.json.
    /// </summary>
    private static bool IsProviderConfigured(IConfigurationSection providerSection)
    {
        var clientId = providerSection["ClientId"];
        var clientSecret = providerSection["ClientSecret"];
        return !string.IsNullOrWhiteSpace(clientId)
            && !string.IsNullOrWhiteSpace(clientSecret)
            && !clientId.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
            && !clientSecret.StartsWith("PLACEHOLDER", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// AUTH-CODE-05 — startup-time provenance for the SSO wiring. Program.cs reads
/// this from DI once the logging pipeline is up and emits a clear warning when
/// the stub was swapped in because real credentials were missing. The class is
/// mutable so it can be populated during <c>AddInfrastructureServices</c> before
/// being registered as a singleton.
/// </summary>
public sealed class SsoConfigurationStatus
{
    public bool UseStubRequested { get; set; }
    public bool FellBackToStub { get; set; }
    public bool MissingGoogle { get; set; }
    public bool MissingMicrosoft { get; set; }
}
