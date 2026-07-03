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
///   <item><c>/api/auth/*</c> — login / refresh stay live so an admin can sign in
///     to turn maintenance off (otherwise the toggle locks the admin out).</item>
///   <item><c>/api/admin/settings</c> — the toggle itself stays reachable.</item>
///   <item><c>/api/profiles/*/photo</c> — generated avatars are cached and
///     fetched without auth; serving 503 here just floods the network tab.</item>
///   <item><c>/scalar</c> and <c>/openapi</c> — dev-only API docs</item>
/// </list>
/// </summary>
public sealed class MaintenanceMiddleware(
    RequestDelegate next,
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MaintenanceMiddleware> logger)
{
    private const string CacheKey = "platform:maintenance.enabled";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    // Paths that bypass maintenance mode — always reachable so that:
    // • Health probes continue to work (load balancer / App Service health)
    // • The client can poll /api/status to detect when maintenance ends
    // • Auth endpoints stay live so an admin can sign in and disable maintenance
    // • The settings PATCH stays live so they can flip the toggle
    // • Dev docs are reachable for API debugging
    private static readonly string[] BypassedPrefixes =
    [
        "/health",
        "/api/status",
        "/api/auth",
        "/api/admin/settings",
        // DES-09: the in-app notifications feed + its SignalR hub stay reachable so
        // signed-in clients can still pull items they missed during a maintenance
        // window. Both are [Authorize]-gated, so nothing unauthenticated is exposed.
        "/api/notifications",
        "/hubs/notifications",
        "/api/profiles",
        "/scalar",
        "/openapi",
    ];

    public async Task Invoke(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        // CORS preflight requests must always succeed — otherwise the browser
        // never even sees the 503 body and just reports a CORS failure. The
        // OPTIONS request carries no body, so passing it through is safe.
        if (HttpMethods.IsOptions(context.Request.Method))
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
            // Surface CORS headers on the 503 so the browser shows the actual
            // body instead of the "blocked by CORS policy" error. Without
            // these the UI can't render a friendly maintenance page either.
            ApplyCorsHeaders(context);
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

    /// <summary>
    /// Mirrors the CORS policy onto the 503 response. The CORS middleware
    /// only runs after this one returns, so we have to set the headers
    /// ourselves whenever we short-circuit the pipeline.
    /// </summary>
    private void ApplyCorsHeaders(HttpContext context)
    {
        var origin = context.Request.Headers.Origin.ToString();
        if (string.IsNullOrEmpty(origin)) return;

        // Read the same allow-list the CORS middleware uses so we stay in sync.
        var allowed = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        if (!allowed.Contains(origin, StringComparer.OrdinalIgnoreCase)) return;

        var headers = context.Response.Headers;
        headers["Access-Control-Allow-Origin"] = origin;
        headers["Access-Control-Allow-Credentials"] = "true";
        headers["Vary"] = "Origin";
    }
}
