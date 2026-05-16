using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;
using Testcontainers.MsSql;
using Xunit;

namespace ScholarPath.IntegrationTests;

public sealed class CustomWebApplicationFactory :
    WebApplicationFactory<Program>,
    IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword("Your_strong_password_123!")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _sqlContainer.GetConnectionString()
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICurrentUserService>();

            services.AddSingleton<TestCurrentUserService>();
            services.AddSingleton<ICurrentUserService>(sp => sp.GetRequiredService<TestCurrentUserService>());

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
            })
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                TestAuthHandler.SchemeName,
                _ => { });
        });
    }

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _sqlContainer.DisposeAsync();
    }
}

public sealed class TestCurrentUserService : ICurrentUserService
{
    private readonly List<string> _roles = [];

    public Guid? UserId { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated { get; set; } = true;

    public string? ActiveRole { get; set; }
    public IReadOnlyCollection<string> Roles => _roles;

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }

    public bool IsInRole(string role) =>
        _roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public void SetUser(Guid userId, string email, params string[] roles)
    {
        UserId = userId;
        Email = email;
        IsAuthenticated = true;

        _roles.Clear();
        _roles.AddRange(roles);

        ActiveRole = roles.FirstOrDefault();
        IpAddress = "127.0.0.1";
        UserAgent = "IntegrationTests";
        CorrelationId = Guid.NewGuid().ToString("N");
    }

    public void Clear()
    {
        UserId = null;
        Email = null;
        IsAuthenticated = false;
        ActiveRole = null;
        IpAddress = null;
        UserAgent = null;
        CorrelationId = null;
        _roles.Clear();
    }
}
