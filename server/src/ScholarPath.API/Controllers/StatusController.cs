using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Public status probe — returns whether the platform is in maintenance mode
/// and the current server time. Consumed by the React client on every boot
/// (and re-polled every 30 s) so the UI can show the maintenance screen
/// without attempting any other authenticated requests.
///
/// This endpoint is intentionally bypassed by <see cref="MaintenanceMiddleware"/>
/// so it always responds, even while the site is offline for maintenance.
/// </summary>
[ApiController]
[Route("api/status")]
[AllowAnonymous]
[Produces("application/json")]
public sealed class StatusController(
    IMemoryCache cache,
    IServiceScopeFactory scopeFactory,
    ILogger<StatusController> logger) : ControllerBase
{
    private const string CacheKey = "platform:maintenance.enabled";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>Returns the current platform status.</summary>
    /// <response code="200">Always succeeds — maintenance flag + server time.</response>
    [HttpGet]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        bool maintenanceModeEnabled;
        try
        {
            maintenanceModeEnabled = await cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var value = await db.PlatformSettings
                    .AsNoTracking()
                    .Where(s => s.Key == "maintenance.enabled")
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
                return bool.TryParse(value, out var flag) && flag;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "StatusController: could not read maintenance setting; defaulting to false.");
            maintenanceModeEnabled = false;
        }

        return Ok(new StatusResponse(
            MaintenanceModeEnabled: maintenanceModeEnabled,
            Version: typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            ServerTime: DateTimeOffset.UtcNow));
    }

    public sealed record StatusResponse(
        bool MaintenanceModeEnabled,
        string Version,
        DateTimeOffset ServerTime);
}
