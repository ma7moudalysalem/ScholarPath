using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;

public sealed class RequestBookingCommandHandler : IRequestHandler<RequestBookingCommand, RequestBookingResult>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;

    public RequestBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
    }

    public async Task<RequestBookingResult> Handle(RequestBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (!_currentUser.IsInRole("Student"))
        {
            throw new UnauthorizedAccessException("Only students can request bookings.");
        }

        var studentId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        if (studentId == request.ConsultantId)
        {
            throw new BookingDomainException("Student cannot book a session with themselves.");
        }

        var scheduledStartAtUtc = request.ScheduledStartAt.ToUniversalTime();
        var scheduledEndAtUtc = request.ScheduledEndAt.ToUniversalTime();

        if (scheduledStartAtUtc >= scheduledEndAtUtc)
        {
            throw new BookingDomainException("ScheduledStartAt must be earlier than ScheduledEndAt.");
        }

        // FR-078/084: a booking can only be requested for a future time.
        if (scheduledStartAtUtc <= DateTimeOffset.UtcNow)
        {
            throw new BookingDomainException("Booking start time must be in the future.");
        }

        var durationMinutes = (int)Math.Round((scheduledEndAtUtc - scheduledStartAtUtc).TotalMinutes);

        if (durationMinutes <= 0)
        {
            throw new BookingDomainException("Booking duration must be greater than zero.");
        }

        var consultant = await _context.Users
            .Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.Id == request.ConsultantId, cancellationToken);

        if (consultant is null)
        {
            throw new BookingDomainException("Consultant was not found.");
        }

        if (consultant.Profile is null)
        {
            throw new BookingDomainException("Consultant profile was not found.");
        }

        if (!consultant.Profile.SessionFeeUsd.HasValue || consultant.Profile.SessionFeeUsd.Value <= 0)
        {
            throw new BookingDomainException("Consultant session fee is not configured.");
        }

        if (consultant.Profile.SessionDurationMinutes.HasValue &&
            consultant.Profile.SessionDurationMinutes.Value != durationMinutes)
        {
            throw new BookingDomainException("Requested duration does not match consultant session duration.");
        }

        // FR-094: a consultant whose booking intake is suspended (low ratings,
        // pending admin review) cannot receive new bookings.
        if (consultant.Profile.BookingIntakeSuspendedAt is not null)
        {
            throw new BookingDomainException(
                "This consultant is not currently accepting new bookings.");
        }

        ConsultantAvailability? availability = null;

        if (request.AvailabilityId.HasValue)
        {
            availability = await _context.Availabilities
                .FirstOrDefaultAsync(
                    a => a.Id == request.AvailabilityId.Value
                         && a.ConsultantId == request.ConsultantId
                         && a.IsActive,
                    cancellationToken);

            if (availability is null)
            {
                throw new BookingDomainException("Availability slot was not found.");
            }

            if (!availability.IsRecurring)
            {
                if (!availability.SpecificStartAt.HasValue || !availability.SpecificEndAt.HasValue)
                {
                    throw new BookingDomainException("Ad-hoc availability slot is invalid.");
                }

                if (scheduledStartAtUtc < availability.SpecificStartAt.Value.ToUniversalTime()
                    || scheduledEndAtUtc > availability.SpecificEndAt.Value.ToUniversalTime())
                {
                    throw new BookingDomainException("Requested booking time is outside the selected availability range.");
                }
            }
            else
            {
                if (availability.DayOfWeek is null
                    || availability.StartTime is null
                    || availability.EndTime is null)
                {
                    throw new BookingDomainException("Recurring availability slot is invalid.");
                }

                // FR-078: validate the requested time against the recurring rule
                // in the consultant's own timezone (the rule's TimeOnly window),
                // so a manipulated client cannot book outside published availability.
                var tz = TimeZoneResolver.Resolve(availability.Timezone);
                var localStart = TimeZoneInfo.ConvertTime(scheduledStartAtUtc, tz);
                var localEnd = TimeZoneInfo.ConvertTime(scheduledEndAtUtc, tz);

                if (localStart.DayOfWeek != availability.DayOfWeek.Value
                    || localStart.Date != localEnd.Date)
                {
                    throw new BookingDomainException(
                        "Requested booking day does not match the consultant's availability.");
                }

                if (TimeOnly.FromTimeSpan(localStart.TimeOfDay) < availability.StartTime.Value
                    || TimeOnly.FromTimeSpan(localEnd.TimeOfDay) > availability.EndTime.Value)
                {
                    throw new BookingDomainException(
                        "Requested booking time is outside the consultant's availability.");
                }
            }
        }

        var blockingStatuses = new[]
        {
            BookingStatus.Requested,
            BookingStatus.Confirmed
        };

        var consultantHasConflict = await _context.Bookings.AnyAsync(
            b => b.ConsultantId == request.ConsultantId
                 && blockingStatuses.Contains(b.Status)
                 && scheduledStartAtUtc < b.ScheduledEndAt
                 && scheduledEndAtUtc > b.ScheduledStartAt,
            cancellationToken);

        if (consultantHasConflict)
        {
            throw new BookingDomainException("Consultant already has a booking that overlaps this time.");
        }

        var studentHasConflict = await _context.Bookings.AnyAsync(
            b => b.StudentId == studentId
                 && blockingStatuses.Contains(b.Status)
                 && scheduledStartAtUtc < b.ScheduledEndAt
                 && scheduledEndAtUtc > b.ScheduledStartAt,
            cancellationToken);

        if (studentHasConflict)
        {
            throw new BookingDomainException("Student already has a booking that overlaps this time.");
        }

        var priceUsd = consultant.Profile.SessionFeeUsd.Value;
        var amountCents = (long)decimal.Round(priceUsd * 100m, 0, MidpointRounding.AwayFromZero);

        if (amountCents <= 0)
        {
            throw new BookingDomainException("Calculated booking amount must be greater than zero.");
        }

        var idempotencyKey =
            $"booking-request:{studentId:N}:{request.ConsultantId:N}:{scheduledStartAtUtc:yyyyMMddHHmm}:{scheduledEndAtUtc:yyyyMMddHHmm}";

        var metadata = new Dictionary<string, string>
        {
            ["feature"] = "consultant-booking",
            ["studentId"] = studentId.ToString("N"),
            ["consultantId"] = request.ConsultantId.ToString("N"),
            ["scheduledStartAtUtc"] = scheduledStartAtUtc.ToString("O"),
            ["scheduledEndAtUtc"] = scheduledEndAtUtc.ToString("O")
        };

        if (request.AvailabilityId.HasValue)
        {
            metadata["availabilityId"] = request.AvailabilityId.Value.ToString("N");
        }

        var paymentIntent = await _stripeService.CreatePaymentIntentAsync(
            amountCents: amountCents,
            currency: "usd",
            captureMethod: "manual",
            metadata: metadata,
            idempotencyKey: idempotencyKey,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(paymentIntent.Id))
        {
            throw new BookingDomainException("Stripe payment intent was not created successfully.");
        }

        var nowUtc = DateTimeOffset.UtcNow;

        var payment = new Payment
        {
            Type = PaymentType.ConsultantBooking,
            // FR-080/186 (PB-006 gap P2): the manual-capture intent is not truly
            // held until the student authorises the card. The Payment starts
            // Pending and the Stripe `amount_capturable_updated` webhook flips it
            // to Held once authorisation actually lands.
            Status = PaymentStatus.Pending,
            AmountCents = amountCents,
            Currency = "USD",
            ProfitShareAmountCents = 0,
            PayeeAmountCents = amountCents,
            RefundedAmountCents = 0,
            PayerUserId = studentId,
            PayeeUserId = request.ConsultantId,
            StripePaymentIntentId = paymentIntent.Id,
            StripeChargeId = paymentIntent.LatestChargeId,
            IdempotencyKey = idempotencyKey,
            RelatedBookingId = null,
            RelatedApplicationId = null,
            HeldAt = null,
            CapturedAt = null,
            RefundedAt = null,
            RefundReason = null,
            FailureReason = null,
            IsDeleted = false,
            DeletedAt = null,
            DeletedByUserId = null
        };

        var booking = new ConsultantBooking
        {
            StudentId = studentId,
            ConsultantId = request.ConsultantId,
            AvailabilityId = request.AvailabilityId,
            ScheduledStartAt = scheduledStartAtUtc,
            ScheduledEndAt = scheduledEndAtUtc,
            DurationMinutes = durationMinutes,
            PriceUsd = priceUsd,
            Status = BookingStatus.Requested,
            RequestedAt = nowUtc,
            StripePaymentIntentId = paymentIntent.Id,
            Payment = payment,
            ConfirmedAt = null,
            RejectedAt = null,
            ExpiredAt = null,
            CancelledAt = null,
            CompletedAt = null,
            CancellationReason = null,
            CancelledByUserId = null,
            IsNoShowStudent = false,
            IsNoShowConsultant = false,
            NoShowMarkedAt = null,
            IsDeleted = false,
            DeletedAt = null,
            DeletedByUserId = null
        };

        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync(cancellationToken);

        payment.RelatedBookingId = booking.Id;
        await _context.SaveChangesAsync(cancellationToken);

        // Return the intent's client secret so the checkout widget confirms
        // THIS intent — it must not create a second one (PB-006 Problem 1).
        return new RequestBookingResult(booking.Id, paymentIntent.ClientSecret, paymentIntent.Id);
    }
}
