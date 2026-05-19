using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.ConsultantBookings.EventHandlers;

/// <summary>
/// Renders bilingual notification text for a booking event by joining the booking,
/// student, and consultant rows once. Shared by every booking handler in this file so
/// each handler stays focused on routing the result to the right recipient.
/// </summary>
internal static class BookingNotificationContext
{
    internal sealed record View(
        string StudentName,
        string ConsultantName,
        DateTimeOffset ScheduledStartAt);

    internal static async Task<View?> LoadAsync(
        IApplicationDbContext db, Guid bookingId, CancellationToken ct)
    {
        var row = await db.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => new
            {
                b.ScheduledStartAt,
                StudentFirst = b.Student!.FirstName,
                StudentLast = b.Student.LastName,
                ConsultantFirst = b.Consultant!.FirstName,
                ConsultantLast = b.Consultant.LastName,
            })
            .FirstOrDefaultAsync(ct);

        if (row is null) return null;

        return new View(
            StudentName: $"{row.StudentFirst} {row.StudentLast}".Trim(),
            ConsultantName: $"{row.ConsultantFirst} {row.ConsultantLast}".Trim(),
            ScheduledStartAt: row.ScheduledStartAt);
    }

    internal static string FormatStartAt(DateTimeOffset startAt) =>
        startAt.ToUniversalTime().ToString("yyyy-MM-dd HH:mm 'UTC'");
}

/// <summary>
/// Notifies the consultant that a new booking is waiting on their review (FR-079).
/// </summary>
public sealed class BookingRequestedEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingRequestedEventHandler> logger)
    : INotificationHandler<BookingRequestedEvent>
{
    public async Task Handle(BookingRequestedEvent notification, CancellationToken ct)
    {
        var view = await BookingNotificationContext.LoadAsync(db, notification.BookingId, ct);
        if (view is null)
        {
            logger.LogWarning(
                "BookingRequested {BookingId} fired but the booking row was not found.",
                notification.BookingId);
            return;
        }

        try
        {
            await notifications.DispatchAsync(
                notification.ConsultantId,
                NotificationType.BookingRequested,
                new NotificationParams
                {
                    CounterpartyName = view.StudentName,
                    StartAtText = BookingNotificationContext.FormatStartAt(view.ScheduledStartAt),
                },
                deepLink: $"/consultant/bookings/{notification.BookingId}",
                idempotencyKey: $"booking-requested:{notification.BookingId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BookingRequested notification failed for booking {BookingId}.",
                notification.BookingId);
        }
    }
}

/// <summary>
/// Notifies the student that their booking was accepted by the consultant (FR-081).
/// </summary>
public sealed class BookingConfirmedEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingConfirmedEventHandler> logger)
    : INotificationHandler<BookingConfirmedEvent>
{
    public async Task Handle(BookingConfirmedEvent notification, CancellationToken ct)
    {
        var view = await BookingNotificationContext.LoadAsync(db, notification.BookingId, ct);
        if (view is null)
        {
            logger.LogWarning(
                "BookingConfirmed {BookingId} fired but the booking row was not found.",
                notification.BookingId);
            return;
        }

        try
        {
            await notifications.DispatchAsync(
                notification.StudentId,
                NotificationType.BookingConfirmed,
                new NotificationParams
                {
                    CounterpartyName = view.ConsultantName,
                    StartAtText = BookingNotificationContext.FormatStartAt(view.ScheduledStartAt),
                },
                deepLink: $"/student/bookings/{notification.BookingId}",
                idempotencyKey: $"booking-confirmed:{notification.BookingId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BookingConfirmed notification failed for booking {BookingId}.",
                notification.BookingId);
        }
    }
}

/// <summary>
/// Notifies the student that the consultant declined their booking (FR-082).
/// </summary>
public sealed class BookingRejectedEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingRejectedEventHandler> logger)
    : INotificationHandler<BookingRejectedEvent>
{
    public async Task Handle(BookingRejectedEvent notification, CancellationToken ct)
    {
        var view = await BookingNotificationContext.LoadAsync(db, notification.BookingId, ct);
        if (view is null)
        {
            logger.LogWarning(
                "BookingRejected {BookingId} fired but the booking row was not found.",
                notification.BookingId);
            return;
        }

        try
        {
            await notifications.DispatchAsync(
                notification.StudentId,
                NotificationType.BookingRejected,
                new NotificationParams { CounterpartyName = view.ConsultantName },
                deepLink: $"/student/bookings/{notification.BookingId}",
                idempotencyKey: $"booking-rejected:{notification.BookingId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BookingRejected notification failed for booking {BookingId}.",
                notification.BookingId);
        }
    }
}

/// <summary>
/// Notifies the party that did not initiate the cancellation (FR-085) — if the
/// student cancelled, the consultant hears about it, and vice versa.
/// </summary>
public sealed class BookingCancelledEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingCancelledEventHandler> logger)
    : INotificationHandler<BookingCancelledEvent>
{
    public async Task Handle(BookingCancelledEvent notification, CancellationToken ct)
    {
        var view = await BookingNotificationContext.LoadAsync(db, notification.BookingId, ct);
        if (view is null)
        {
            logger.LogWarning(
                "BookingCancelled {BookingId} fired but the booking row was not found.",
                notification.BookingId);
            return;
        }

        var cancelledByStudent = notification.CancelledByUserId == notification.StudentId;
        var recipientId = cancelledByStudent ? notification.ConsultantId : notification.StudentId;
        var counterpartyName = cancelledByStudent ? view.StudentName : view.ConsultantName;
        var deepLink = cancelledByStudent
            ? $"/consultant/bookings/{notification.BookingId}"
            : $"/student/bookings/{notification.BookingId}";

        try
        {
            await notifications.DispatchAsync(
                recipientId,
                NotificationType.BookingCancelled,
                new NotificationParams
                {
                    CounterpartyName = counterpartyName,
                    StartAtText = BookingNotificationContext.FormatStartAt(view.ScheduledStartAt),
                    Reason = notification.Reason,
                },
                deepLink: deepLink,
                idempotencyKey: $"booking-cancelled:{notification.BookingId:N}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BookingCancelled notification failed for booking {BookingId}.",
                notification.BookingId);
        }
    }
}

/// <summary>
/// Notifies both parties when the CompletionJob auto-completes a session so each
/// can leave a rating.
/// </summary>
public sealed class BookingCompletedEventHandler(
    IApplicationDbContext db,
    INotificationDispatcher notifications,
    ILogger<BookingCompletedEventHandler> logger)
    : INotificationHandler<BookingCompletedEvent>
{
    public async Task Handle(BookingCompletedEvent notification, CancellationToken ct)
    {
        var view = await BookingNotificationContext.LoadAsync(db, notification.BookingId, ct);
        if (view is null)
        {
            logger.LogWarning(
                "BookingCompleted {BookingId} fired but the booking row was not found.",
                notification.BookingId);
            return;
        }

        await SafeDispatchAsync(
            recipientId: notification.StudentId,
            counterpartyName: view.ConsultantName,
            deepLink: $"/student/bookings/{notification.BookingId}",
            suffix: "student",
            bookingId: notification.BookingId,
            ct: ct);

        await SafeDispatchAsync(
            recipientId: notification.ConsultantId,
            counterpartyName: view.StudentName,
            deepLink: $"/consultant/bookings/{notification.BookingId}",
            suffix: "consultant",
            bookingId: notification.BookingId,
            ct: ct);
    }

    private async Task SafeDispatchAsync(
        Guid recipientId, string counterpartyName, string deepLink,
        string suffix, Guid bookingId, CancellationToken ct)
    {
        try
        {
            await notifications.DispatchAsync(
                recipientId,
                NotificationType.BookingCompleted,
                new NotificationParams { CounterpartyName = counterpartyName },
                deepLink: deepLink,
                idempotencyKey: $"booking-completed:{bookingId:N}:{suffix}",
                ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "BookingCompleted notification failed for booking {BookingId} -> {Recipient}.",
                bookingId, suffix);
        }
    }
}
