using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.MsSql;

namespace ScholarPath.IntegrationTests.Scholarships;

public class ScholarshipApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder().Build();

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) => {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _dbContainer.GetConnectionString()
            });
        });
    }

    // Blocker I7: استخدام الـ override فقط وإلغاء استدعاء الـ Interface المزدوج
    public override async ValueTask DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;
}
