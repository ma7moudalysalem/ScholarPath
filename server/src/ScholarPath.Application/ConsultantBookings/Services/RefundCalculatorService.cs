using ScholarPath.Domain.Enums;

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
        decimal priceUsd,
        DateTimeOffset nowUtc)
    {
        var amountCents = (long)decimal.Round(priceUsd * 100m, 0, MidpointRounding.AwayFromZero);

        if (amountCents < 0)
        {
            throw new InvalidOperationException("Booking price cannot be negative.");
        }

        var cancelledByStudent = cancelledByUserId == studentId;
        var cancelledByConsultant = cancelledByUserId == consultantId;

        if (!cancelledByStudent && !cancelledByConsultant)
        {
            throw new InvalidOperationException("Cancelling user is not related to this booking.");
        }

        if (bookingStatus == BookingStatus.Requested)
        {
            if (!cancelledByStudent)
            {
                throw new InvalidOperationException("Requested bookings can only be cancelled by the student. Consultants should reject them.");
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
            throw new InvalidOperationException("Only requested or confirmed bookings can be cancelled.");
        }

        if (cancelledByConsultant)
        {
            return new RefundCalculationResult
            {
                RefundPercentage = 100,
                RefundAmountCents = amountCents,
                CancellationReason = CancellationReason.ConsultantCancelledAfterAcceptance
            };
        }

        var moreThan24HoursBefore = scheduledStartAt.ToUniversalTime() > nowUtc.AddHours(24);

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
