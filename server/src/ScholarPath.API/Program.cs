using System.Threading.RateLimiting;
using Asp.Versioning;
using Hangfire;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using ScholarPath.API.Middleware;
using ScholarPath.Application;
using ScholarPath.Infrastructure;
using ScholarPath.Infrastructure.Persistence;
using Serilog;
using FluentValidation;
using ScholarPath.Application.Auth.DTOs;
using ScholarPath.Application.Auth.Validators;

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

    // SSO Validators
    builder.Services.AddScoped<IValidator<ExternalLoginRequest>, ExternalLoginRequestValidator>();
    builder.Services.AddScoped<IValidator<LinkProviderRequest>, LinkProviderRequestValidator>();

    // Register Google/Microsoft OAuth schemes 
    var authBuilder = builder.Services.AddAuthentication();

    var googleClientId = builder.Configuration["ExternalAuth:Google:ClientId"];
    var googleClientSecret = builder.Configuration["ExternalAuth:Google:ClientSecret"];
    if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
    {
        authBuilder.AddGoogle("Google", options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SaveTokens = true;
        });
    }

    var msClientId = builder.Configuration["ExternalAuth:Microsoft:ClientId"];
    var msClientSecret = builder.Configuration["ExternalAuth:Microsoft:ClientSecret"];
    if (!string.IsNullOrWhiteSpace(msClientId) && !string.IsNullOrWhiteSpace(msClientSecret))
    {
        authBuilder.AddMicrosoftAccount("Microsoft", options =>
        {
            options.ClientId = msClientId;
            options.ClientSecret = msClientSecret;
            options.SaveTokens = true;
        });
    }

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
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "ScholarPath API", Version = "v1" });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter: Bearer {your JWT token}"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
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

    // DEV DB bootstrap
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        db.Database.EnsureCreated();

        // Seed Data
        await SeedData.InitializeAsync(scope.ServiceProvider);
    }

    app.MapControllers();

    // Health checks
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program;
