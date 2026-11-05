using System.Data.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScholarPath.Infrastructure.Diagnostics;

// ─────────────────────────────────────────────────────────────────────────────
// Live "System Activity" diagnostics — a defense/ops view that streams every
// API request and the SQL each one runs to admins in real time (SignalR) and to
// the log stream. Designed to be SAFE for production:
//   • Master switch (Diagnostics:Enabled, default true) can hard-disable it.
//   • Self-activating: nothing is captured or broadcast unless an admin actually
//     has the page open (subscriber count > 0), so idle overhead is a bool read.
//   • Admin/SuperAdmin only (the hub is role-gated).
//   • SQL parameter VALUES are never read — only the parameterised command text
//     (EnableSensitiveDataLogging stays false), so no user data leaks.
//   • Every capture path is wrapped so diagnostics can never break a request.
// ─────────────────────────────────────────────────────────────────────────────

public sealed class DiagnosticsOptions
{
    public const string SectionName = "Diagnostics";
    public bool Enabled { get; set; } = true;
}

/// <summary>Flows a short id from the request middleware down to the EF interceptor
/// (same async context) so a SQL row can be tied back to the API call that ran it.</summary>
public static class RequestCorrelation
{
    private static readonly AsyncLocal<string?> _id = new();
    public static string? Current
    {
        get => _id.Value;
        set => _id.Value = value;
    }
}

public sealed record ApiRequestActivity(
    string Id,
    string Time,
    string Method,
    string Path,
    int Status,
    long DurationMs,
    string User,
    string Role);

public sealed record SqlQueryActivity(
    string Id,
    string Time,
    string Sql,
    long DurationMs,
    string Kind);

public interface ISystemActivityBroadcaster
{
    /// <summary>True only when the feature is enabled AND at least one admin is watching.</summary>
    bool IsActive { get; }
    void AddSubscriber();
    void RemoveSubscriber();
    void PublishRequest(ApiRequestActivity activity);
    void PublishSql(SqlQueryActivity activity);
}

public sealed class SystemActivityBroadcaster(
    IHubContext<SystemActivityHub> hub,
    IOptions<DiagnosticsOptions> options,
    ILogger<SystemActivityBroadcaster> logger) : ISystemActivityBroadcaster
{
    private int _subscribers;
    private readonly bool _enabled = options.Value.Enabled;

    public bool IsActive => _enabled && Volatile.Read(ref _subscribers) > 0;

    public void AddSubscriber() => Interlocked.Increment(ref _subscribers);

    public void RemoveSubscriber()
    {
        if (Interlocked.Decrement(ref _subscribers) < 0)
            Interlocked.Exchange(ref _subscribers, 0);
    }

    public void PublishRequest(ApiRequestActivity activity) => Fire("ApiRequest", activity);

    public void PublishSql(SqlQueryActivity activity) => Fire("SqlQuery", activity);

    // Fire-and-forget so diagnostics never adds latency to (or throws into) the
    // request/query path. Swallows all errors — this is best-effort telemetry.
    private void Fire(string evt, object payload)
    {
        if (!IsActive) return;
        _ = Task.Run(async () =>
        {
            try { await hub.Clients.All.SendAsync(evt, payload).ConfigureAwait(false); }
            catch (Exception ex) { logger.LogDebug(ex, "System-activity broadcast failed for {Event}", evt); }
        });
    }
}

/// <summary>Admin-only hub the System Activity page connects to. Presence here is
/// what turns capture on/off in the broadcaster.</summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public sealed class SystemActivityHub(ISystemActivityBroadcaster broadcaster) : Hub
{
    public override Task OnConnectedAsync()
    {
        broadcaster.AddSubscriber();
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        broadcaster.RemoveSubscriber();
        return base.OnDisconnectedAsync(exception);
    }
}

/// <summary>Captures the parameterised text + duration of every executed SQL command
/// (only while an admin is watching) and streams it, correlated to the API request.</summary>
public sealed class DiagnosticsSqlInterceptor(ISystemActivityBroadcaster broadcaster) : DbCommandInterceptor
{
    private const int MaxSqlLength = 4000;

    private void Capture(DbCommand command, CommandExecutedEventData eventData, string kind)
    {
        if (!broadcaster.IsActive) return;
        try
        {
            var sql = command.CommandText ?? string.Empty;
            if (sql.Length > MaxSqlLength) sql = sql[..MaxSqlLength] + " …";
            broadcaster.PublishSql(new SqlQueryActivity(
                RequestCorrelation.Current ?? "—",
                DateTimeOffset.UtcNow.ToString("O"),
                sql,
                (long)eventData.Duration.TotalMilliseconds,
                kind));
        }
        catch
        {
            // Never let telemetry interfere with data access.
        }
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Capture(command, eventData, "reader");
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData, "reader");
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Capture(command, eventData, "write");
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData, "write");
        return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Capture(command, eventData, "scalar");
        return base.ScalarExecuted(command, eventData, result);
    }

    public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData, "scalar");
        return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }
}
