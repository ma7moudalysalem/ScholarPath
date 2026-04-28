using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Application.Common.Interfaces;
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
        services.AddSingleton<ISsoService, StubSsoService>();

        // Email, blob, Stripe, AI, notifications, audit — all stubbed by default
        services.AddSingleton<IEmailService, StubEmailService>();
        services.AddSingleton<IBlobStorageService, StubBlobStorageService>();
        services.AddSingleton<IStripeService, StubStripeService>();
        // LocalAiService is deterministic, no-network — our default. Teammates
        // swap to OpenAI by setting Ai:Provider = "OpenAi" and wiring a real impl.
        services.AddScoped<IAiService, LocalAiService>();
        services.AddScoped<INotificationDispatcher, StubNotificationDispatcher>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IUserAdministration, UserAdministration>();
        services.AddScoped<IAdminReadService, AdminReadService>();

        // Jobs
        services.AddScoped<IDeadlineReminderJob, DeadlineReminderJob>();
        services.AddScoped<INotificationDispatcherJob, NotificationDispatcherJob>();
        services.AddScoped<IStripePayoutJob, StripePayoutJob>();
        services.AddScoped<ISessionExpiryJob, SessionExpiryJob>();
        services.AddScoped<IIntegrityCheckJob, IntegrityCheckJob>();
        services.AddScoped<IDataExportJob, DataExportJob>();
        services.AddScoped<IDataDeleteJob, DataDeleteJob>();

        // Memory cache (Redis swap handled at deploy time)
        services.AddMemoryCache();

        return services;
    }
}
