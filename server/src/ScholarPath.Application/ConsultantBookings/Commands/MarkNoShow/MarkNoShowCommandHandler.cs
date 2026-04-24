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

    public MarkNoShowCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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
