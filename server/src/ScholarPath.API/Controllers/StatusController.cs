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
    private const string MaintenanceCacheKey = "platform:maintenance.enabled";
    private const string PaymentsCacheKey = "platform:payments.enabled";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    /// <summary>Returns the current platform status.</summary>
    /// <response code="200">Always succeeds — maintenance flag + server time.</response>
    [HttpGet]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var maintenanceModeEnabled = await ReadBoolSettingAsync(
            MaintenanceCacheKey, "maintenance.enabled", defaultValue: false, ct);

        var paymentsEnabled = await ReadBoolSettingAsync(
            PaymentsCacheKey, "payments.enabled", defaultValue: true, ct);

        return Ok(new StatusResponse(
            MaintenanceModeEnabled: maintenanceModeEnabled,
            PaymentsEnabled: paymentsEnabled,
            Version: typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0",
            ServerTime: DateTimeOffset.UtcNow));
    }

    /// <summary>
    /// 30 s in-memory cache around a single boolean platform setting — the
    /// status endpoint is polled cheaply on every page load, so we don't want
    /// to hit Postgres for each call.
    /// </summary>
    private async Task<bool> ReadBoolSettingAsync(
        string cacheKey, string settingKey, bool defaultValue, CancellationToken ct)
    {
        try
        {
            return await cache.GetOrCreateAsync(cacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheTtl;
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var value = await db.PlatformSettings
                    .AsNoTracking()
                    .Where(s => s.Key == settingKey)
                    .Select(s => s.Value)
                    .FirstOrDefaultAsync(ct)
                    .ConfigureAwait(false);
                return bool.TryParse(value, out var flag) ? flag : defaultValue;
            }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "StatusController: could not read {SettingKey}; defaulting to {Default}.",
                settingKey, defaultValue);
            return defaultValue;
        }
    }

    public sealed record StatusResponse(
        bool MaintenanceModeEnabled,
        bool PaymentsEnabled,
        string Version,
        DateTimeOffset ServerTime);
}
