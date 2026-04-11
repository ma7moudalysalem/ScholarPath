using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ScholarPath.Infrastructure.Hubs;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/diagnostics")]
[Produces("application/json")]
public sealed class DiagnosticsController(IHubContext<NotificationHub> notificationHub) : ControllerBase
{
    /// <summary>Smoke endpoint: emits a test notification via SignalR to the caller.</summary>
    [HttpPost("test-notification")]
    [Authorize]
    public async Task<IActionResult> PushTestNotification(CancellationToken ct)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        await notificationHub.Clients.Group($"user:{userId}").SendAsync("notification", new
        {
            titleEn = "Test notification",
            titleAr = "إشعار تجريبي",
            bodyEn = "This is a test broadcast from the diagnostics endpoint.",
            bodyAr = "هذا إشعار اختبار من واجهة التشخيص.",
            createdAt = DateTimeOffset.UtcNow,
        }, ct).ConfigureAwait(false);

        return Accepted(new { dispatched = true, targetUserId = userId });
    }

    /// <summary>Confirm service is alive (no auth needed for readiness probes).</summary>
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { status = "ok", at = DateTimeOffset.UtcNow });
}
