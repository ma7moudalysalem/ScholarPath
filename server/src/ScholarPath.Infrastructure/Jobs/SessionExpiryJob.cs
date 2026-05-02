using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Jobs;

public sealed class SessionExpiryJob : ISessionExpiryJob
{
    private readonly IApplicationDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly ILogger<SessionExpiryJob> _logger;

    public SessionExpiryJob(
        IApplicationDbContext context,
        IStripeService stripeService,
        ILogger<SessionExpiryJob> logger)
    {
        _context = context;
        _stripeService = stripeService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var expiryThreshold = DateTimeOffset.UtcNow.AddHours(-24);

        var bookings = await _context.Bookings
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
