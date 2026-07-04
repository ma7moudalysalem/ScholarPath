using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;

namespace ScholarPath.Application.ConsultantBookings.Services;

public sealed class RefundCalculationResult
{
    public int RefundPercentage { get; init; }
    public long RefundAmountCents { get; init; }
    public CancellationReason CancellationReason { get; init; }
}

public sealed class RefundCalculatorService
{
    public RefundCalculationResult Calculate(
        BookingStatus bookingStatus,
        Guid cancelledByUserId,
        Guid studentId,
        Guid consultantId,
        DateTimeOffset scheduledStartAt,
        long amountCents,
        DateTimeOffset nowUtc)
    {
        // FR-198/199: the refund basis is the original payment amount stored on
        // the Payment row, not the booking price — the two can diverge later
        // (discounts, currency, re-pricing), so the stored snapshot is authoritative.
        if (amountCents < 0)
        {
            throw new BookingDomainException("Booking amount cannot be negative.");
        }

        var cancelledByStudent = cancelledByUserId == studentId;
        var cancelledByConsultant = cancelledByUserId == consultantId;

        if (!cancelledByStudent && !cancelledByConsultant)
        {
            throw new BookingDomainException("Cancelling user is not related to this booking.");
        }

        if (bookingStatus == BookingStatus.Requested)
        {
            if (!cancelledByStudent)
            {
                throw new BookingDomainException("Requested bookings can only be cancelled by the student. Consultants should reject them.");
            }

            return new RefundCalculationResult
            {
                RefundPercentage = 100,
                RefundAmountCents = amountCents,
                CancellationReason = CancellationReason.StudentCancelledBeforeAcceptance
            };
        }

        if (bookingStatus != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only requested or confirmed bookings can be cancelled.");
        }

        var moreThan24HoursBefore = scheduledStartAt.ToUniversalTime() > nowUtc.AddHours(24);

        if (cancelledByConsultant)
        {
            // The student is always fully refunded when the consultant cancels; the
            // <24h case additionally carries a rating penalty (applied by the handler).
            return new RefundCalculationResult
            {
                RefundPercentage = 100,
                RefundAmountCents = amountCents,
                CancellationReason = moreThan24HoursBefore
                    ? CancellationReason.ConsultantCancelledAfterAcceptance
                    : CancellationReason.ConsultantCancelledLessThan24Hours
            };
        }

        if (moreThan24HoursBefore)
        {
            return new RefundCalculationResult
            {
                RefundPercentage = 100,
                RefundAmountCents = amountCents,
                CancellationReason = CancellationReason.StudentCancelledMoreThan24HoursBefore
            };
        }

        return new RefundCalculationResult
        {
            RefundPercentage = 50,
            RefundAmountCents = amountCents / 2,
            CancellationReason = CancellationReason.StudentCancelledLessThan24HoursBefore
        };
    }
}
