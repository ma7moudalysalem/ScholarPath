using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using Testcontainers.MsSql;
using Xunit;

namespace ScholarPath.IntegrationTests.Payments;

public sealed class PaymentsWebApplicationFactory
    : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_password_123!")
        .Build();

    public Guid SeededStudentId { get; } = Guid.NewGuid();
    public Guid SeededConsultantId { get; } = Guid.NewGuid();
    public Guid SeededAdminId { get; } = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    _sqlContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace real CurrentUserService with test version
            services.RemoveAll<ICurrentUserService>();
            services.AddSingleton<PaymentsTestCurrentUserService>();
            services.AddSingleton<ICurrentUserService>(sp =>
                sp.GetRequiredService<PaymentsTestCurrentUserService>());

            // Replace JWT with test auth scheme
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    PaymentsTestAuthHandler.SchemeName;
                options.DefaultChallengeScheme =
                    PaymentsTestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, PaymentsTestAuthHandler>(
                PaymentsTestAuthHandler.SchemeName, _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        // Migrate outside the app's retrying execution strategy (see TestDatabase).
        await TestDatabase.MigrateAsync(_sqlContainer.GetConnectionString());

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        await SeedUsersAsync(db);
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }

    public HttpClient CreateAuthenticatedClient(
        Guid? userId = null,
        string role = "Student")
    {
        var resolvedId = userId ?? SeededStudentId;

        // Set the user BEFORE creating the client
        var userService = Services
            .GetRequiredService<PaymentsTestCurrentUserService>();

        userService.SetUser(resolvedId, $"{role.ToLower()}@test.com", role);

        return CreateClient();
    }
    private async Task SeedUsersAsync(ApplicationDbContext db)
    {
        // Use unique suffix per factory instance to avoid UserNameIndex conflicts
        // when multiple test classes share the same DB
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var users = new[]
        {
        new ApplicationUser
        {
            Id                 = SeededStudentId,
            UserName           = $"student_{suffix}@test.com",
            NormalizedUserName = $"STUDENT_{suffix.ToUpper()}@TEST.COM",
            Email              = $"student_{suffix}@test.com",
            NormalizedEmail    = $"STUDENT_{suffix.ToUpper()}@TEST.COM",
            EmailConfirmed     = true,
            FirstName          = "Test",
            LastName           = "Student",
            AccountStatus      = AccountStatus.Active,
            ActiveRole         = "Student"
        },
        new ApplicationUser
        {
            Id                 = SeededConsultantId,
            UserName           = $"consultant_{suffix}@test.com",
            NormalizedUserName = $"CONSULTANT_{suffix.ToUpper()}@TEST.COM",
            Email              = $"consultant_{suffix}@test.com",
            NormalizedEmail    = $"CONSULTANT_{suffix.ToUpper()}@TEST.COM",
            EmailConfirmed     = true,
            FirstName          = "Test",
            LastName           = "Consultant",
            AccountStatus      = AccountStatus.Active,
            ActiveRole         = "Consultant"
        },
        new ApplicationUser
        {
            Id                 = SeededAdminId,
            UserName           = $"admin_{suffix}@test.com",
            NormalizedUserName = $"ADMIN_{suffix.ToUpper()}@TEST.COM",
            Email              = $"admin_{suffix}@test.com",
            NormalizedEmail    = $"ADMIN_{suffix.ToUpper()}@TEST.COM",
            EmailConfirmed     = true,
            FirstName          = "Test",
            LastName           = "Admin",
            AccountStatus      = AccountStatus.Active,
            ActiveRole         = "Admin"
        }
    };

        foreach (var user in users
            .Where(u => !db.Users.Any(x => x.Id == u.Id)))
        {
            db.Users.Add(user);
        }

        await db.SaveChangesAsync();
    }


}
