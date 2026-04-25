using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;

public sealed class MarkNoShowCommandHandler : IRequestHandler<MarkNoShowCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;

    public MarkNoShowCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
    }

    public async Task Handle(MarkNoShowCommand request, CancellationToken cancellationToken)
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
            throw new UnauthorizedAccessException("You are not allowed to mark no-show for this booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new InvalidOperationException("Only confirmed bookings can be marked as no-show.");
        }

        if (booking.IsNoShowStudent || booking.IsNoShowConsultant ||
            booking.Status == BookingStatus.NoShowStudent ||
            booking.Status == BookingStatus.NoShowConsultant)
        {
            throw new InvalidOperationException("This booking already has a no-show mark.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var sessionEndUtc = booking.ScheduledEndAt.ToUniversalTime();

        if (nowUtc < sessionEndUtc)
        {
            throw new InvalidOperationException("No-show can only be marked after the session end time.");
        }

        if (nowUtc > sessionEndUtc.AddHours(6))
        {
            throw new InvalidOperationException("No-show can only be marked within 6 hours after session end.");
        }

        booking.NoShowMarkedAt = nowUtc;

        if (isStudent)
        {
            if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
            {
                throw new InvalidOperationException("Booking has no Stripe payment intent to refund.");
            }

            var amountCents = (long)decimal.Round(
                booking.PriceUsd * 100m,
                0,
                MidpointRounding.AwayFromZero);

            if (amountCents <= 0)
            {
                throw new InvalidOperationException("Booking amount must be greater than zero.");
            }

            var idempotencyKey = $"booking-noshow-refund:{booking.Id:N}";

            var refundResult = await _stripeService.RefundPaymentAsync(
                paymentIntentId: booking.StripePaymentIntentId,
                amountCents: amountCents,
                reason: CancellationReason.ConsultantNoShow.ToString(),
                idempotencyKey: idempotencyKey,
                ct: cancellationToken);

            if (string.IsNullOrWhiteSpace(refundResult.Id))
            {
                throw new InvalidOperationException("Stripe refund failed.");
            }

            booking.IsNoShowConsultant = true;
            booking.Status = BookingStatus.NoShowConsultant;
            booking.CancellationReason = CancellationReason.ConsultantNoShow;
        }
        else
        {
            booking.IsNoShowStudent = true;
            booking.Status = BookingStatus.NoShowStudent;
            booking.CancellationReason = CancellationReason.StudentNoShow;
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
