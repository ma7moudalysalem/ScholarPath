using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
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
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingDomainException("Booking was not found.");
        }

        var isStudent = booking.StudentId == currentUserId;
        var isConsultant = booking.ConsultantId == currentUserId;

        if (!isStudent && !isConsultant)
        {
            throw new UnauthorizedAccessException("You are not allowed to mark no-show for this booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only confirmed bookings can be marked as no-show.");
        }

        if (booking.IsNoShowStudent || booking.IsNoShowConsultant ||
            booking.Status == BookingStatus.NoShowStudent ||
            booking.Status == BookingStatus.NoShowConsultant)
        {
            throw new BookingDomainException("This booking already has a no-show mark.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var sessionEndUtc = booking.ScheduledEndAt.ToUniversalTime();

        if (nowUtc < sessionEndUtc)
        {
            throw new BookingDomainException("No-show can only be marked after the session end time.");
        }

        if (nowUtc > sessionEndUtc.AddHours(6))
        {
            throw new BookingDomainException("No-show can only be marked within 6 hours after session end.");
        }

        booking.NoShowMarkedAt = nowUtc;

        if (isStudent)
        {
            // Free booking (no Stripe intent, no Payment row): the student is
            // still entitled to mark the consultant as a no-show — there's just
            // no money to refund. Skip the Stripe + Payment-row updates.
            var isFree = booking.PriceUsd == 0m && booking.Payment is null;

            if (!isFree)
            {
                if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
                {
                    throw new BookingDomainException("Booking has no Stripe payment intent to refund.");
                }

                // Refund the amount that was actually captured (the Payment row
                // is the source of truth), not a re-derivation from PriceUsd —
                // the two can diverge if the booking was re-priced after payment,
                // which would otherwise over- or under-refund on Stripe while the
                // Payment row claims a full refund.
                var amountCents = booking.Payment is { } capturedPayment
                    ? capturedPayment.AmountCents
                    : (long)decimal.Round(booking.PriceUsd * 100m, 0, MidpointRounding.AwayFromZero);

                if (amountCents < 0)
                {
                    throw new BookingDomainException("Booking amount cannot be negative.");
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
                    throw new BookingDomainException("Stripe refund failed.");
                }

                // FR-090/193: a consultant no-show fully refunds the student, so the
                // internal Payment row is marked Refunded for the gross amount and
                // both commission and payee net are zeroed (retained = 0).
                if (booking.Payment is { } payment)
                {
                    payment.Status = PaymentStatus.Refunded;
                    payment.RefundedAmountCents = payment.AmountCents;
                    payment.RefundedAt = nowUtc;
                    payment.RefundReason = CancellationReason.ConsultantNoShow.ToString();
                    payment.ProfitShareAmountCents = 0;
                    payment.PayeeAmountCents = 0;
                }
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
