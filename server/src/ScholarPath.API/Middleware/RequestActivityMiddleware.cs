using System.Diagnostics;
using System.Security.Claims;
using ScholarPath.Infrastructure.Diagnostics;

namespace ScholarPath.API.Middleware;

/// <summary>
/// Streams a compact record of each API request (method, path, status, duration,
/// user) to the live System Activity view, and tags the request with a short id
/// that the SQL interceptor picks up so queries correlate to the call that ran
/// them. Does nothing unless an admin is actively watching (broadcaster.IsActive),
/// so it is free in normal operation. Only /api/* requests are tracked — hub
/// traffic and static assets are ignored.
/// </summary>
public sealed class RequestActivityMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context, ISystemActivityBroadcaster broadcaster)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (!broadcaster.IsActive || !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
        {
            await next(context).ConfigureAwait(false);
            return;
        }

        var id = Guid.NewGuid().ToString("N")[..8];
        RequestCorrelation.Current = id;
        var sw = Stopwatch.StartNew();
        try
        {
            await next(context).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            try
            {
                var user = context.User;
                broadcaster.PublishRequest(new ApiRequestActivity(
                    id,
                    DateTimeOffset.UtcNow.ToString("O"),
                    context.Request.Method,
                    // Path only: query strings carry reset/confirm tokens and search terms.
                    path,
                    context.Response.StatusCode,
                    (long)sw.Elapsed.TotalMilliseconds,
                    user?.Identity?.IsAuthenticated == true
                        ? (user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue(ClaimTypes.Email) ?? "authenticated")
                        : "anonymous",
                    user?.FindFirstValue("active_role") ?? user?.FindFirstValue(ClaimTypes.Role) ?? "—"));
            }
            catch
            {
                // best-effort telemetry — never fail the request on a reporting error
            }
            RequestCorrelation.Current = null;
        }
    }
}
