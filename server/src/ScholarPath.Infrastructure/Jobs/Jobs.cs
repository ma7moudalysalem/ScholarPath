using Microsoft.Extensions.Logging;

namespace ScholarPath.Infrastructure.Jobs;

/// <summary>Hangfire job stubs. Teammates fill the logic from their module specs.</summary>

public interface IDeadlineReminderJob
{
    Task RunAsync(CancellationToken ct);
}

public sealed class DeadlineReminderJob(ILogger<DeadlineReminderJob> logger) : IDeadlineReminderJob
{
    public Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("[job] DeadlineReminderJob tick (stub)");
        return Task.CompletedTask;
    }
}

public interface INotificationDispatcherJob
{
    Task RunAsync(CancellationToken ct);
}

public sealed class NotificationDispatcherJob(ILogger<NotificationDispatcherJob> logger) : INotificationDispatcherJob
{
    public Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("[job] NotificationDispatcherJob tick (stub)");
        return Task.CompletedTask;
    }
}

public interface IStripePayoutJob
{
    Task RunAsync(CancellationToken ct);
}

public sealed class StripePayoutJob(ILogger<StripePayoutJob> logger) : IStripePayoutJob
{
    public Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("[job] StripePayoutJob tick (stub)");
        return Task.CompletedTask;
    }
}

public interface ISessionExpiryJob
{
    Task RunAsync(CancellationToken ct);
}

public sealed class SessionExpiryJob(ILogger<SessionExpiryJob> logger) : ISessionExpiryJob
{
    public Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("[job] SessionExpiryJob tick (stub)");
        return Task.CompletedTask;
    }
}

public interface IIntegrityCheckJob
{
    Task RunAsync(CancellationToken ct);
}

public sealed class IntegrityCheckJob(ILogger<IntegrityCheckJob> logger) : IIntegrityCheckJob
{
    public Task RunAsync(CancellationToken ct)
    {
        logger.LogInformation("[job] IntegrityCheckJob tick (stub)");
        return Task.CompletedTask;
    }
}
