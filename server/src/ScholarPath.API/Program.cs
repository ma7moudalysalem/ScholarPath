using System.Text;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using Hangfire;
using Hangfire.MemoryStorage;
using Hangfire.SqlServer;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using ScholarPath.API.Middleware;
using ScholarPath.Application;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure;
using ScholarPath.Infrastructure.Hubs;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Persistence.Seed;
using ScholarPath.Infrastructure.Settings;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ─── Serilog ─────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, sp, config) =>
    config.ReadFrom.Configuration(ctx.Configuration)
          .ReadFrom.Services(sp)
          .Enrich.FromLogContext()
          .Enrich.WithEnvironmentName()
          .Enrich.WithThreadId());

// ─── Controllers + JSON ──────────────────────────────────────────────────────
builder.Services
    .AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        opts.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddFluentValidationAutoValidation().AddFluentValidationClientsideAdapters();

// ─── Application + Infrastructure DI ─────────────────────────────────────────
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// ─── JWT Authentication ──────────────────────────────────────────────────────
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt section missing.");

builder.Services
    .AddAuthentication(opts =>
    {
        opts.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        opts.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(opts =>
    {
        opts.SaveToken = true;
        opts.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
        // Allow JWT via query string for SignalR
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                var path = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

// ─── CORS ────────────────────────────────────────────────────────────────────
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(p => p
        .WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ─── Rate limiting ───────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("auth", httpContext =>
        System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 10,
                QueueLimit = 0,
            }));
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ─── SignalR ─────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ─── Swagger / Scalar ────────────────────────────────────────────────────────
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opts =>
{
    opts.SwaggerDoc("v1", new OpenApiInfo { Title = "ScholarPath API", Version = "v1", Description = "Gated scholarship platform." });

    opts.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste: {token}",
    });
    opts.AddSecurityRequirement((Microsoft.OpenApi.OpenApiDocument doc) => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer"), new List<string>() },
    });
});

// ─── Health checks ───────────────────────────────────────────────────────────
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// ─── Hangfire (feature-flagged off by default) ───────────────────────────────
var hangfireOpts = builder.Configuration.GetSection(HangfireOptions.SectionName).Get<HangfireOptions>() ?? new();
if (hangfireOpts.Enabled)
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddHangfire(cfg =>
    {
        cfg.UseSimpleAssemblyNameTypeSerializer()
           .UseRecommendedSerializerSettings();
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            cfg.UseSqlServerStorage(connectionString, new SqlServerStorageOptions
            {
                CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                QueuePollInterval = TimeSpan.FromSeconds(30),
            });
        }
        else
        {
            cfg.UseMemoryStorage();
        }
    });
    builder.Services.AddHangfireServer();
}

// ─── Build app ───────────────────────────────────────────────────────────────
var app = builder.Build();

// ─── Pipeline ────────────────────────────────────────────────────────────────
app.UseSerilogRequestLogging();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlerMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(opts => opts.RouteTemplate = "openapi/{documentName}.json");
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "ScholarPath API";
        opts.OpenApiRoutePattern = "/openapi/{documentName}.json";
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();

// Health
app.MapHealthChecks("/health");

// SignalR hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<CommunityHub>("/hubs/community");

// Hangfire dashboard (Admin-only) when enabled
if (hangfireOpts.Enabled && hangfireOpts.DashboardEnabled)
{
    app.MapHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new AdminDashboardAuthorizationFilter()],
    });
}

// Recurring jobs — only scheduled when Hangfire is enabled
if (hangfireOpts.Enabled)
{
    var recurring = app.Services.GetRequiredService<IRecurringJobManager>();
    recurring.AddOrUpdate<IDataExportJob>("data-export-sweep", j => j.RunAsync(CancellationToken.None), Cron.Hourly);
    recurring.AddOrUpdate<IDataDeleteJob>("data-delete-sweep", j => j.RunAsync(CancellationToken.None), Cron.Daily(3)); // 03:00 UTC
    recurring.AddOrUpdate<IIntegrityCheckJob>("integrity-check",  j => j.RunAsync(CancellationToken.None), Cron.Daily(4));
    recurring.AddOrUpdate<ISessionExpiryJob>("session-expiry",    j => j.RunAsync(CancellationToken.None), "*/15 * * * *"); // every 15 min
    recurring.AddOrUpdate<IStripePayoutJob>("stripe-payouts",     j => j.RunAsync(CancellationToken.None), Cron.Daily(2));
    recurring.AddOrUpdate<IDeadlineReminderJob>("deadline-reminders", j => j.RunAsync(CancellationToken.None), Cron.Daily(9));
    // PB-017 FR-254 — monthly PII-redaction sampling. First day of the month at 02:00 UTC.
    recurring.AddOrUpdate<IRedactionAuditSamplingJob>("redaction-audit-sampling", j => j.RunAsync(CancellationToken.None), "0 2 1 * *");
}

// ─── Seed database ───────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
    try
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var um = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var rm = sp.GetRequiredService<RoleManager<ApplicationRole>>();
        if (app.Environment.IsDevelopment())
        {
            await DbSeeder.SeedAsync(db, um, rm, logger, CancellationToken.None).ConfigureAwait(false);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database seed failed.");
    }
}

app.Run();

// Make Program discoverable for WebApplicationFactory-based integration tests
public partial class Program;

internal sealed class AdminDashboardAuthorizationFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // Delegate to reflection since Hangfire.Dashboard.DashboardContext APIs differ across versions.
        var ctxType = context.GetType();
        var httpCtxProp = ctxType.GetProperty("HttpContext")
            ?? ctxType.BaseType?.GetProperty("HttpContext");
        if (httpCtxProp?.GetValue(context) is Microsoft.AspNetCore.Http.HttpContext http)
        {
            return http.User.Identity?.IsAuthenticated == true && http.User.IsInRole("Admin");
        }
        return false;
    }
}
