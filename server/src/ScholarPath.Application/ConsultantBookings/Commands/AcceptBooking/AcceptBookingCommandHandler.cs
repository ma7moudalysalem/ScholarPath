using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;

public sealed class AcceptBookingCommandHandler : IRequestHandler<AcceptBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;
    private readonly IMeetingService _meetingService;
    private readonly IPublisher _publisher;

    public AcceptBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService,
        IMeetingService meetingService,
        IPublisher publisher)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
        _meetingService = meetingService;
        _publisher = publisher;
    }

    public async Task Handle(AcceptBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (!_currentUser.IsInRole("Consultant"))
        {
            throw new UnauthorizedAccessException("Only consultants can accept bookings.");
        }

        var consultantId = _currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var booking = await _context.Bookings
            .Include(b => b.Payment)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new BookingDomainException("Booking was not found.");
        }

        if (booking.ConsultantId != consultantId)
        {
            throw new UnauthorizedAccessException("You are not allowed to accept this booking.");
        }

        if (booking.Status != BookingStatus.Requested)
        {
            throw new BookingDomainException("Only requested bookings can be accepted.");
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new BookingDomainException("Booking has no Stripe payment intent to capture.");
        }

        var amountCents = (long)decimal.Round(
            booking.PriceUsd * 100m,
            0,
            MidpointRounding.AwayFromZero);

        if (amountCents <= 0)
        {
            throw new BookingDomainException("Booking amount must be greater than zero.");
        }

        var idempotencyKey = $"booking-accept:{booking.Id:N}";

        var captureResult = await _stripeService.CapturePaymentIntentAsync(
            paymentIntentId: booking.StripePaymentIntentId,
            amountToCaptureCents: amountCents,
            idempotencyKey: idempotencyKey,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(captureResult.Id))
        {
            throw new BookingDomainException("Stripe payment capture failed.");
        }

        var nowUtc = DateTimeOffset.UtcNow;

        // FR-081/187/190: sync the internal Payment row with the Stripe capture —
        // mark it Captured and lock in the platform/payee split from the financial
        // rule in force, so payment history, payouts and reporting stay accurate.
        // Accepts Held or Pending: the Stripe capture above already proved the
        // intent was authorised, even if the Held webhook has not landed yet (P2).
        if (booking.Payment is { Status: PaymentStatus.Held or PaymentStatus.Pending } payment)
        {
            payment.Status = PaymentStatus.Captured;
            payment.CapturedAt = nowUtc;
            if (!string.IsNullOrWhiteSpace(captureResult.LatestChargeId))
            {
                payment.StripeChargeId = captureResult.LatestChargeId;
            }

            var split = await FinancialRuleResolver.ResolvePaymentSplitAsync(
                _context, payment.Type, payment.AmountCents, cancellationToken);
            payment.ProfitShareAmountCents = split.PlatformTakeCents;
            payment.PayeeAmountCents = split.PayeeNetCents;
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = nowUtc;

        // PB-006 — provision the video-meeting room for the confirmed session.
        // Each participant gets a join token from the booking's meeting room.
        var room = await _meetingService.CreateRoomAsync(booking.Id, cancellationToken);
        booking.MeetingRoomId = room.RoomId;

        await _context.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new BookingConfirmedEvent(
                booking.Id,
                booking.StudentId,
                booking.ConsultantId),
            cancellationToken);
    }
}
