namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Recurring sweep that fires reminder notifications (BookingReminder, 24-hour and
/// 1-hour variants) to the student and consultant before a Confirmed session.
/// Idempotency is provided by the notification dispatcher's IdempotencyKey lookup,
/// so a re-run after the job was down still sends each reminder exactly once.
/// </summary>
public interface IBookingReminderJob
{
    Task RunAsync(CancellationToken cancellationToken);
}
