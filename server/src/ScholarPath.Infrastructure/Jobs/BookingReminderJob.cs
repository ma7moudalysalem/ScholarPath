using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

/// <summary>
/// Recurring sweep (PB-006) — fires BookingReminder notifications to the student and
/// the consultant before a Confirmed session.
///
/// Two reminder kinds:
///   • <c>24h</c> — once when the session is within the next 24 hours and still more
///     than 1 hour away.
///   • <c>1h</c> — once when the session is within the next 1 hour.
///
/// Idempotency is keyed per booking × kind × recipient, so the dispatcher's
/// IdempotencyKey lookup prevents duplicates even if the job is re-run, replayed,
/// or has been down for a while. Late reminders (job was paused) are still allowed
/// as long as the session has not started and the reminder was not sent yet.
/// </summary>
public sealed class BookingReminderJob(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingReminderJob> logger) : IBookingReminderJob
{
    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var oneHourOut = now.AddHours(1);
        var twentyFourHoursOut = now.AddHours(24);

        // Pull every Confirmed booking whose start window we still care about: future
        // start within the next 24 hours. The narrow window keeps the projection cheap.
        var upcoming = await db.Bookings
            .Where(b => b.Status == BookingStatus.Confirmed
                && b.ScheduledStartAt > now
                && b.ScheduledStartAt <= twentyFourHoursOut)
            .Select(b => new UpcomingBooking(
                b.Id,
                b.StudentId,
                b.ConsultantId,
                b.ScheduledStartAt,
                (b.Student!.FirstName + " " + b.Student.LastName).Trim(),
                (b.Consultant!.FirstName + " " + b.Consultant.LastName).Trim()))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (upcoming.Count == 0)
        {
            logger.LogInformation("[booking-reminder] no upcoming bookings in the next 24h.");
            return;
        }

        var sent = 0;
        foreach (var booking in upcoming)
        {
            // Within the final hour: only the 1h reminder is in scope. Anything past
            // its start is filtered out by the query above.
            if (booking.ScheduledStartAt <= oneHourOut)
            {
                sent += await DispatchAsync(booking, kind: "1h", cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // ScheduledStartAt > now + 1h AND <= now + 24h
                sent += await DispatchAsync(booking, kind: "24h", cancellationToken).ConfigureAwait(false);
            }
        }

        logger.LogInformation(
            "[booking-reminder] run complete — {Bookings} booking(s) in window, {Sent} reminder(s) dispatched.",
            upcoming.Count, sent);
    }

    private async Task<int> DispatchAsync(
        UpcomingBooking booking, string kind, CancellationToken ct)
    {
        var startAtText = booking.ScheduledStartAt.ToUniversalTime()
            .ToString("yyyy-MM-dd HH:mm 'UTC'");
        var dispatched = 0;

        // Student-facing reminder — counterparty is the consultant.
        try
        {
            await notifications.DispatchAsync(
                booking.StudentId,
                NotificationType.BookingReminder,
                new NotificationParams
                {
                    CounterpartyName = booking.ConsultantName,
                    StartAtText = startAtText,
                    ReminderKind = kind,
                },
                deepLink: $"/student/bookings/{booking.Id}",
                idempotencyKey: $"booking-reminder:{booking.Id:N}:{kind}:student",
                ct).ConfigureAwait(false);
            dispatched++;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[booking-reminder] {Kind} dispatch failed for booking {BookingId} -> student.",
                kind, booking.Id);
        }

        // Consultant-facing reminder — counterparty is the student.
        try
        {
            await notifications.DispatchAsync(
                booking.ConsultantId,
                NotificationType.BookingReminder,
                new NotificationParams
                {
                    CounterpartyName = booking.StudentName,
                    StartAtText = startAtText,
                    ReminderKind = kind,
                },
                deepLink: $"/consultant/bookings/{booking.Id}",
                idempotencyKey: $"booking-reminder:{booking.Id:N}:{kind}:consultant",
                ct).ConfigureAwait(false);
            dispatched++;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[booking-reminder] {Kind} dispatch failed for booking {BookingId} -> consultant.",
                kind, booking.Id);
        }

        return dispatched;
    }

    private sealed record UpcomingBooking(
        Guid Id,
        Guid StudentId,
        Guid ConsultantId,
        DateTimeOffset ScheduledStartAt,
        string StudentName,
        string ConsultantName);
}
