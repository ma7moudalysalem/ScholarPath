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

public interface IIntegrityCheckJob
{
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Daily sweep for orphan / inconsistent rows. Surfaces as warnings that
/// the admin dashboard rolls up.
/// </summary>
public sealed class IntegrityCheckJob(
    Persistence.ApplicationDbContext db,
    ILogger<IntegrityCheckJob> logger) : IIntegrityCheckJob
{
    public async Task RunAsync(CancellationToken ct)
    {
        var orphanPayments = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Payments
                .Where(p => p.RelatedBookingId == null && p.RelatedApplicationId == null), ct)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var overdueBookings = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.Bookings
                .Where(b => b.Status == Domain.Enums.BookingStatus.Confirmed
                    && b.ScheduledEndAt < now.AddHours(-6)), ct)
            .ConfigureAwait(false);

        var stuckWebhooks = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(db.StripeWebhookEvents
                .Where(e => !e.IsProcessed && e.ProcessingAttempts >= 5), ct)
            .ConfigureAwait(false);

        if (orphanPayments > 0 || overdueBookings > 0 || stuckWebhooks > 0)
        {
            logger.LogWarning(
                "[integrity] orphanPayments={OrphanPayments} overdueBookings={OverdueBookings} stuckWebhooks={StuckWebhooks}",
                orphanPayments, overdueBookings, stuckWebhooks);
        }
        else
        {
            logger.LogInformation("[integrity] clean sweep");
        }
    }
}
