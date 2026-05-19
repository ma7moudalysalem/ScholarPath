using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

public sealed class SessionExpiryJob : ISessionExpiryJob
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SessionExpiryJob> _logger;
    private readonly int _responseWindowHours;

    public SessionExpiryJob(
        IApplicationDbContext context,
        IStripeService stripeService,
        IOptions<BookingOptions> bookingOptions,
        ILogger<SessionExpiryJob> logger)
    {
        _context = context;
        _stripeService = stripeService;
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire booking {BookingId}.", booking.Id);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("SessionExpiryJob processed {Count} expired requested bookings.", bookings.Count);
    }
}
