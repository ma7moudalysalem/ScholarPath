using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;

public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;
    private readonly RefundCalculatorService _refundCalculator;

    public CancelBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService,
        RefundCalculatorService refundCalculator)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
        _refundCalculator = refundCalculator;
    }

    public async Task Handle(CancelBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var currentUserId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new InvalidOperationException("Booking was not found.");
        }

        var isStudent = booking.StudentId == currentUserId;
        var isConsultant = booking.ConsultantId == currentUserId;

        if (!isStudent && !isConsultant)
        {
            throw new UnauthorizedAccessException("You are not allowed to cancel this booking.");
        }

        if (booking.Status != BookingStatus.Requested && booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only requested or confirmed bookings can be cancelled.");
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new InvalidOperationException("Booking has no Stripe payment intent.");
        }

        var nowUtc = DateTimeOffset.UtcNow;

        var refund = _refundCalculator.Calculate(
            bookingStatus: booking.Status,
            cancelledByUserId: currentUserId,
            studentId: booking.StudentId,
            consultantId: booking.ConsultantId,
            scheduledStartAt: booking.ScheduledStartAt,
            priceUsd: booking.PriceUsd,
            nowUtc: nowUtc);

        if (booking.Status == BookingStatus.Requested)
        {
            var cancelIdempotencyKey = $"booking-cancel-requested:{booking.Id:N}";

            var cancelResult = await _stripeService.CancelPaymentIntentAsync(
                paymentIntentId: booking.StripePaymentIntentId,
                cancellationReason: "requested_booking_cancelled",
                idempotencyKey: cancelIdempotencyKey,
                ct: cancellationToken);

            if (string.IsNullOrWhiteSpace(cancelResult.Id))
            {
                throw new InvalidOperationException("Stripe payment intent cancellation failed.");
            }
        }
        else
        {
            var refundIdempotencyKey = $"booking-refund:{booking.Id:N}:{refund.RefundPercentage}";

            var refundResult = await _stripeService.RefundPaymentAsync(
                paymentIntentId: booking.StripePaymentIntentId,
                amountCents: refund.RefundAmountCents,
                reason: refund.CancellationReason.ToString(),
                idempotencyKey: refundIdempotencyKey,
                ct: cancellationToken);

            if (string.IsNullOrWhiteSpace(refundResult.Id))
            {
                throw new InvalidOperationException("Stripe refund failed.");
            }
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = nowUtc;
        booking.CancelledByUserId = currentUserId;
        booking.CancellationReason = refund.CancellationReason;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
