using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;

public sealed class RejectBookingCommandHandler : IRequestHandler<RejectBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IStripeService _stripeService;

    public RejectBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IStripeService stripeService)
    {
        _context = context;
        _currentUser = currentUser;
        _stripeService = stripeService;
    }

    public async Task Handle(RejectBookingCommand request, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        if (!_currentUser.IsInRole("Consultant"))
        {
            throw new UnauthorizedAccessException("Only consultants can reject bookings.");
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
            throw new UnauthorizedAccessException("You are not allowed to reject this booking.");
        }

        if (booking.Status != BookingStatus.Requested)
        {
            throw new InvalidOperationException("Only requested bookings can be rejected.");
        }

        if (string.IsNullOrWhiteSpace(booking.StripePaymentIntentId))
        {
            throw new InvalidOperationException("Booking has no Stripe payment intent to cancel.");
        }

        var idempotencyKey = $"booking-reject:{booking.Id:N}";

        var cancelResult = await _stripeService.CancelPaymentIntentAsync(
            paymentIntentId: booking.StripePaymentIntentId,
            cancellationReason: "rejected_by_consultant",
            idempotencyKey: idempotencyKey,
            ct: cancellationToken);

        if (string.IsNullOrWhiteSpace(cancelResult.Id))
        {
            throw new InvalidOperationException("Stripe payment intent cancellation failed.");
        }

        booking.Status = BookingStatus.Rejected;
        booking.RejectedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }
}
