using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ConsultantBookings;
using ScholarPath.Application.ConsultantBookings.Services;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;

public sealed class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;
    private readonly RefundCalculatorService _refundCalculator;
    private readonly ConsultantRatingService _ratingService;
    private readonly BookingOptions _options;
    private readonly IPublisher _publisher;

    public CancelBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService,
        RefundCalculatorService refundCalculator,
        ConsultantRatingService ratingService,
        IOptions<BookingOptions> bookingOptions,
        IPublisher publisher)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
        _refundCalculator = refundCalculator;
        _ratingService = ratingService;
        _options = bookingOptions.Value;
        _publisher = publisher;
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
            throw new UnauthorizedAccessException("You are not allowed to cancel this booking.");
        }

        if (booking.Status != BookingStatus.Requested && booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only requested or confirmed bookings can be cancelled.");
        }

        // Free booking (no Stripe intent, no Payment row): the refund
        // calculator and Stripe steps don't apply — just flip state.
        if (booking.PriceUsd == 0m && booking.Payment is null)
        {
            // Capture the lifecycle phase BEFORE mutating Status — otherwise the
            // reason check below always reads "Cancelled" and every free-booking
            // cancellation is mis-recorded as a consultant-side cancellation.
            var wasRequested = booking.Status == BookingStatus.Requested;
            var nowUtcFree = DateTimeOffset.UtcNow;

            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = nowUtcFree;
            booking.CancelledByUserId = currentUserId;
            // Mirror the paid-path reasoning so free bookings incur the same
            // reputational penalties (money is separate from reputation). A
            // Requested cancel is "before acceptance" (no penalty); a Confirmed
            // cancel <24h before start penalises the cancelling party.
            booking.CancellationReason = ClassifyCancellation(
                isStudent, wasRequested, booking.ScheduledStartAt, nowUtcFree);

            await ApplyReputationPenaltyAsync(booking, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await _publisher.Publish(
                new BookingCancelledEvent(
                    booking.Id,
                    booking.StudentId,
                    booking.ConsultantId,
                    currentUserId,
                    booking.CancellationReason?.ToString()),
                cancellationToken);
            return;
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new BookingDomainException("Booking has no Stripe payment intent.");
        }

        var payment = booking.Payment
            ?? throw new BookingDomainException("Booking has no linked payment record.");

        var nowUtc = DateTimeOffset.UtcNow;

        // FR-198/199: refund amounts are derived from the Payment's stored
        // financial snapshot, not the (possibly stale) booking price.
        var refund = _refundCalculator.Calculate(
            bookingStatus: booking.Status,
            cancelledByUserId: currentUserId,
            studentId: booking.StudentId,
            consultantId: booking.ConsultantId,
            scheduledStartAt: booking.ScheduledStartAt,
            amountCents: payment.AmountCents,
            nowUtc: nowUtc);

        if (booking.Status == BookingStatus.Requested)
        {
            var cancelIdempotencyKey = $"booking-cancel-requested:{booking.Id:N}";

            var cancelResult = await _stripeService.CancelPaymentIntentAsync(
                paymentIntentId: booking.StripePaymentIntentId,
                cancellationReason: "requested_by_customer",
                idempotencyKey: cancelIdempotencyKey,
                ct: cancellationToken);

            if (string.IsNullOrWhiteSpace(cancelResult.Id))
            {
                throw new BookingDomainException("Stripe payment intent cancellation failed.");
            }

            // FR-085/188: release the hold on the internal Payment row.
            if (payment.Status is PaymentStatus.Held or PaymentStatus.Pending)
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.FailureReason = "student_cancelled_before_acceptance";
            }
        }
        else
        {
            var refundIdempotencyKey = $"booking-refund:{booking.Id:N}:{refund.RefundPercentage}";

            // FR-087-089/193: distinguish captured vs. authorized-only payments.
            // A manual-capture PaymentIntent that is confirmed but not yet captured
            // sits in `requires_capture` state on Stripe — creating a Refund object
            // against it would fail with a Stripe 400 ("charge has not been captured").
            // In that case the correct Stripe action is to cancel the intent (which
            // releases the hold on the student's card), not to refund it.
            if (payment.Status == PaymentStatus.Captured)
            {
                var refundResult = await _stripeService.RefundPaymentAsync(
                    paymentIntentId: booking.StripePaymentIntentId,
                    amountCents: refund.RefundAmountCents,
                    reason: "requested_by_customer",
                    idempotencyKey: refundIdempotencyKey,
                    ct: cancellationToken);

                if (string.IsNullOrWhiteSpace(refundResult.Id))
                {
                    throw new BookingDomainException("Stripe refund failed.");
                }

                payment.RefundedAmountCents = refund.RefundAmountCents;
                payment.RefundedAt = nowUtc;
                payment.RefundReason = refund.CancellationReason.ToString();
                payment.Status = refund.RefundAmountCents >= payment.AmountCents
                    ? PaymentStatus.Refunded
                    : PaymentStatus.PartiallyRefunded;

                // PB-014 v1: commission applies to retained amount, not gross.
                // Re-resolve the split off (gross - refunded) so platform take
                // and payee net shrink together with the refund. The resolver
                // returns zero/zero for a full refund.
                var retainedSplit = await FinancialRuleResolver.ResolveSplitFromRetainedAsync(
                    _context, payment.Type,
                    payment.AmountCents, payment.RefundedAmountCents,
                    cancellationToken);
                payment.ProfitShareAmountCents = retainedSplit.PlatformTakeCents;
                payment.PayeeAmountCents = retainedSplit.PayeeNetCents;
            }
            else
            {
                // Payment is authorized but not yet captured (Held / Pending).
                // Cancel the intent so Stripe releases the authorization immediately.
                var cancelIdempotencyKey = $"booking-cancel-confirmed:{booking.Id:N}";

                var cancelResult = await _stripeService.CancelPaymentIntentAsync(
                    paymentIntentId: booking.StripePaymentIntentId,
                    cancellationReason: "requested_by_customer",
                    idempotencyKey: cancelIdempotencyKey,
                    ct: cancellationToken);

                if (string.IsNullOrWhiteSpace(cancelResult.Id))
                {
                    throw new BookingDomainException("Stripe payment intent cancellation failed.");
                }

                payment.Status = PaymentStatus.Cancelled;
                payment.FailureReason = "cancelled_before_capture";
            }
        }

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = nowUtc;
        booking.CancelledByUserId = currentUserId;
        booking.CancellationReason = refund.CancellationReason;

        await ApplyReputationPenaltyAsync(booking, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new BookingCancelledEvent(
                booking.Id,
                booking.StudentId,
                booking.ConsultantId,
                currentUserId,
                booking.CancellationReason?.ToString()),
            cancellationToken);
    }

    /// <summary>
    /// Mirrors <see cref="RefundCalculatorService"/> semantics for the free-booking
    /// path so both paths record the same precise cancellation reason.
    /// </summary>
    private static CancellationReason ClassifyCancellation(
        bool cancelledByStudent, bool wasRequested, DateTimeOffset scheduledStartAt, DateTimeOffset nowUtc)
    {
        if (wasRequested)
        {
            return cancelledByStudent
                ? CancellationReason.StudentCancelledBeforeAcceptance
                : CancellationReason.ConsultantCancelledAfterAcceptance;
        }

        var moreThan24HoursBefore = scheduledStartAt.ToUniversalTime() > nowUtc.AddHours(24);
        return cancelledByStudent
            ? (moreThan24HoursBefore
                ? CancellationReason.StudentCancelledMoreThan24HoursBefore
                : CancellationReason.StudentCancelledLessThan24HoursBefore)
            : (moreThan24HoursBefore
                ? CancellationReason.ConsultantCancelledAfterAcceptance
                : CancellationReason.ConsultantCancelledLessThan24Hours);
    }

    /// <summary>
    /// PB-006R (FR-CBR-15..20): a &lt;24h cancellation of a confirmed booking carries a
    /// reputational penalty on the cancelling party — a 3-day booking block for the
    /// student, a 20% rating deduction for the consultant. &gt;24h and before-acceptance
    /// cancels carry no penalty. Keyed on the recorded reason so both the paid and
    /// free paths behave identically.
    /// </summary>
    private async Task ApplyReputationPenaltyAsync(ConsultantBooking booking, CancellationToken ct)
    {
        switch (booking.CancellationReason)
        {
            case CancellationReason.StudentCancelledLessThan24HoursBefore:
                var studentProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(p => p.UserId == booking.StudentId, ct);
                if (studentProfile is not null)
                {
                    BookingBlockService.ApplyBlock(
                        studentProfile, BookingBlockReason.CancelledLessThan24Hours,
                        _options.LateCancellationBlockDays, DateTimeOffset.UtcNow);
                }
                break;

            case CancellationReason.ConsultantCancelledLessThan24Hours:
                await _ratingService.ApplyPenaltyFactorAsync(
                    booking.ConsultantId, ConsultantRatingThresholds.CancelLessThan24HoursFactor, ct);
                break;
        }
    }
}
