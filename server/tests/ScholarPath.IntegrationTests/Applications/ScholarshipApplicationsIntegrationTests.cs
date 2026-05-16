using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.IntegrationTests.Helpers;
using Testcontainers.MsSql;
using Xunit;

namespace ScholarPath.IntegrationTests.Applications;

public class ScholarshipApplicationsFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // ── Testcontainers ────────────────────────────────────────────────────────
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private bool _containerDisposed;

    // ── Student seed data ─────────────────────────────────────────────────────
    public Guid SeededStudentId { get; } = Guid.NewGuid();

    // ── IAsyncLifetime ────────────────────────────────────────────────────────
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

        await SeedTestStudentAsync(db);
    }

    // IAsyncLifetime.DisposeAsync must return Task (not ValueTask)
    async Task IAsyncLifetime.DisposeAsync()
    {
        if (!_containerDisposed)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
            _containerDisposed = true;
        }
    }

    // ── WebApplicationFactory ─────────────────────────────────────────────────
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    public HttpClient CreateStudentClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Bearer",
                TestJwtHelper.GenerateStudentToken(SeededStudentId));
        return client;
    }

    private async Task SeedTestStudentAsync(ApplicationDbContext db)
    {
        if (db.Users.Any(u => u.Id == SeededStudentId))
            return;

        var student = new ApplicationUser
        {
            Id = SeededStudentId,
            UserName = "teststudent@scholarpath.test",
            Email = "teststudent@scholarpath.test",
            AccountStatus = AccountStatus.Active
        };

        db.Users.Add(student);
        await db.SaveChangesAsync();
    }

    // ── Disposal ──────────────────────────────────────────────────────────────
    public override async ValueTask DisposeAsync()
    {
        if (!_containerDisposed)
        {
            await _dbContainer.StopAsync();
            await _dbContainer.DisposeAsync();
            _containerDisposed = true;
        }

        await base.DisposeAsync();
    }
}
