using System.Threading.RateLimiting;
using Asp.Versioning;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using ScholarPath.Infrastructure.Jobs;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi;
using ScholarPath.API.Middleware;
using ScholarPath.Application;
using ScholarPath.Infrastructure;
using ScholarPath.Infrastructure.Persistence;
using Serilog;

// Serilog bootstrap logger
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .WriteTo.File("logs/scholarpath-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting ScholarPath API...");

    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("logs/scholarpath-.log", rollingInterval: RollingInterval.Day));

    // Application & Infrastructure services
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // Controllers
    builder.Services.AddControllers();

    // CORS
    builder.Services.AddCors(options =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? throw new InvalidOperationException("Cors:AllowedOrigins configuration is required.");

        options.AddPolicy("DefaultCors", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-Api-Version"));
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    });

    // Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ScholarPath API",
            Version = "v1",
            Description = "Scholarship platform API with role-based access for Students, Consultants, Companies, and Admins."
        });

        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter your JWT token"
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("Bearer"),
                new List<string>()
            }
        });
    });

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 20
                }));
    });

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database");

    var app = builder.Build();

    // Exception handling middleware
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    // Security headers middleware
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Serilog request logging
    app.UseSerilogRequestLogging();

    // Swagger (dev only)
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "ScholarPath API v1");
            options.RoutePrefix = "swagger";
        });

        // Hangfire dashboard (dev only)
        var hangfireEnabled = builder.Configuration.GetValue<bool>("Hangfire:Enabled");
        if (hangfireEnabled)
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                Authorization = [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
            });

            RecurringJob.AddOrUpdate<DeadlineReminderJob>(
                "deadline-reminders",
                job => job.ExecuteAsync(),
                Cron.Hourly);
        }
    }

    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });

    app.UseHttpsRedirection();

    app.UseCors("DefaultCors");

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    // Health checks
    app.MapHealthChecks("/health");

    // SignalR hubs
    // app.MapHub<NotificationHub>("/hubs/notifications");
    // app.MapHub<ChatHub>("/hubs/chat");

    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
