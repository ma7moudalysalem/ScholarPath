using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.API.Middleware;

/// <summary>
/// Returns HTTP 503 with <c>{ "maintenance": true }</c> for every request while
/// the <c>maintenance.enabled</c> platform setting is <c>"true"</c>.
///
/// The setting is read from the database at most once every 30 seconds
/// (via <see cref="IMemoryCache"/>) to avoid a DB hit on every request.
///
/// Bypassed paths (always served normally):
/// <list type="bullet">
///   <item><c>/health</c> — ASP.NET health check endpoint</item>
///   <item><c>/api/status</c> — public status / maintenance probe</item>
///   <item><c>/scalar</c> and <c>/openapi</c> — dev-only API docs</item>
/// </list>
/// </summary>
public sealed class MaintenanceMiddleware(
    RequestDelegate next,
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory,
    ILogger<MaintenanceMiddleware> logger)
{
    private const string CacheKey = "platform:maintenance.enabled";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    // Paths that bypass maintenance mode — always reachable so that:
    // • Health probes continue to work (load balancer / App Service health)
    // • The client can poll /api/status to detect when maintenance ends
    // • Dev docs are reachable for API debugging
    private static readonly HashSet<string> BypassedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/api/status",
        "/scalar",
        "/openapi",
    };

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        bool isInMaintenance;
        try
        {
            isInMaintenance = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var value = await db.PlatformSettings
                    .AsNoTracking()
                    .Where(s => s.Key == "maintenance.enabled")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync()
                    .ConfigureAwait(false);
                return bool.TryParse(value, out var flag) && flag;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // On DB error, fail open — don't block legitimate requests.
            logger.LogWarning(ex, "MaintenanceMiddleware: could not read maintenance setting; failing open.");
            isInMaintenance = false;
        }

        if (isInMaintenance)
        {
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                maintenance = true,
                title = "The platform is temporarily offline for maintenance. Please try again shortly.",
            }).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
