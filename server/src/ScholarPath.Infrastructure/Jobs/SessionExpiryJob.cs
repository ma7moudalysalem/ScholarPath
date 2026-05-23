using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

public sealed class SessionExpiryJob : ISessionExpiryJob
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly INotificationDispatcher _notifications;
    private readonly ILogger<SessionExpiryJob> _logger;
    private readonly int _responseWindowHours;

    public SessionExpiryJob(
        IApplicationDbContext context,
        IStripeService stripeService,
        INotificationDispatcher notifications,
        IOptions<BookingOptions> bookingOptions,
        ILogger<SessionExpiryJob> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _notifications = notifications;
        _responseWindowHours = bookingOptions.Value.ConsultantResponseWindowHours;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // FR-083: the consultant response window is configurable (Booking:ConsultantResponseWindowHours).
        var expiryThreshold = DateTimeOffset.UtcNow.AddHours(-_responseWindowHours);

        var bookings = await _context.Bookings
            .Include(b => b.Payment)
            .Where(b =>
                b.Status == BookingStatus.Requested &&
                b.RequestedAt.HasValue &&
                b.RequestedAt.Value <= expiryThreshold)
            .ToListAsync(cancellationToken);

        if (bookings.Count == 0)
        {
            _logger.LogInformation("SessionExpiryJob found no expired requested bookings.");
            return;
        }

        var expired = new List<(Guid BookingId, Guid StudentId)>();
        foreach (var booking in bookings)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
                {
                    var idempotencyKey = $"booking-expire:{booking.Id:N}";

                    await _stripeService.CancelPaymentIntentAsync(
                        paymentIntentId: booking.StripePaymentIntentId,
                        cancellationReason: "abandoned",
                        idempotencyKey: idempotencyKey,
                        ct: cancellationToken);
                }

                // FR-082/086/188: release the internal Payment hold so an expired
                // request is not left financially Held in reports.
                if (booking.Payment is { Status: PaymentStatus.Held or PaymentStatus.Pending } payment)
                {
                    payment.Status = PaymentStatus.Cancelled;
                    payment.FailureReason = "booking_request_expired";
                }

                booking.Status = BookingStatus.Expired;
                booking.ExpiredAt = DateTimeOffset.UtcNow;
                expired.Add((booking.Id, booking.StudentId));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire booking {BookingId}.", booking.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Tell each student their request lapsed and the hold was released — the
        // expiry was previously silent (BookingExpired was defined but never sent).
        // A delivery failure here must not undo the (already-committed) expiry.
        foreach (var (bookingId, studentId) in expired)
        {
            try
            {
                await _notifications.DispatchAsync(
                    studentId,
                    NotificationType.BookingExpired,
                    new NotificationParams(),
                    deepLink: "/student/bookings",
                    idempotencyKey: $"booking-expired-notif:{bookingId:N}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send BookingExpired notification for {BookingId}.", bookingId);
            }
        }

        _logger.LogInformation("SessionExpiryJob processed {Count} expired requested bookings.", bookings.Count);
    }
}
