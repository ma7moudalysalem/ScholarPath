using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;

public sealed class AcceptBookingCommandHandler : IRequestHandler<AcceptBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;

    public AcceptBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
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
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking is null)
        {
            throw new InvalidOperationException("Booking was not found.");
        }

        if (booking.ConsultantId != consultantId)
        {
            throw new UnauthorizedAccessException("You are not allowed to accept this booking.");
        }

        if (booking.Status != BookingStatus.Requested)
        {
            throw new InvalidOperationException("Only requested bookings can be accepted.");
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new InvalidOperationException("Booking has no Stripe payment intent to capture.");
        }

        var amountCents = (long)decimal.Round(booking.PriceUsd * 100m, 0, MidpointRounding.AwayFromZero);

        if (amountCents <= 0)
        {
            throw new InvalidOperationException("Booking amount must be greater than zero.");
        }

        var idempotencyKey = $"booking-accept:{booking.Id:N}";

        var captureResult = await _stripeService.CapturePaymentIntentAsync(
            paymentIntentId: booking.StripePaymentIntentId,
            amountToCaptureCents: amountCents,
            idempotencyKey: idempotencyKey,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(captureResult.Id))
        {
            throw new InvalidOperationException("Stripe payment capture failed.");
        }

        booking.Status = BookingStatus.Confirmed;
        booking.ConfirmedAt = DateTimeOffset.UtcNow;
        booking.MeetingUrl = request.MeetingUrl;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
