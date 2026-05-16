using Microsoft.EntityFrameworkCore;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.IntegrationTests;

/// <summary>
/// Applies EF Core migrations for integration tests using a DbContext that has
/// NO retrying execution strategy.
///
/// The application DbContext is registered with connection resiliency
/// (<c>EnableRetryOnFailure</c>). Under that strategy a transient fault while a
/// migration is being applied makes EF replay the migration from the start —
/// and migrations are not idempotent, so the replay fails with
/// "There is already an object named '...'". Migrations must therefore run
/// exactly once, against a ready database, outside the retry strategy.
/// </summary>
internal static class TestDatabase
{
    public static async Task MigrateAsync(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var db = new ApplicationDbContext(options);

        // The SQL Server container can accept TCP connections slightly before it
        // is ready for DDL — poll until a real connection succeeds.
        for (var attempt = 1; attempt <= 30; attempt++)
        {
            try
            {
                if (await db.Database.CanConnectAsync())
                {
                    break;
                }
            }
            catch
            {
                // container still warming up
            }

            if (attempt == 30)
            {
                throw new InvalidOperationException(
                    "SQL Server test container did not become ready in time.");
            }

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        await db.Database.MigrateAsync();
    }
}
