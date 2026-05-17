using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using ScholarPath.Domain.Exceptions;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;

public sealed class RescheduleBookingCommandHandler : IRequestHandler<RescheduleBookingCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUser;
    private readonly IPublisher _publisher;

    public RescheduleBookingCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUser,
        IPublisher publisher)
    {
        _context = context;
        _currentUser = currentUser;
        _publisher = publisher;
    }

    public async Task Handle(RescheduleBookingCommand request, CancellationToken cancellationToken)
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
            throw new BookingDomainException("Booking was not found.");
        }

        var isStudent = booking.StudentId == currentUserId;
        var isConsultant = booking.ConsultantId == currentUserId;

        if (!isStudent && !isConsultant)
        {
            throw new UnauthorizedAccessException("You are not allowed to reschedule this booking.");
        }

        // Only a live booking can move — a rejected/expired/cancelled/completed
        // booking (or one with a no-show outcome) is terminal.
        if (booking.Status != BookingStatus.Requested && booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only requested or confirmed bookings can be rescheduled.");
        }

        var scheduledStartAtUtc = request.ScheduledStartAt.ToUniversalTime();
        var scheduledEndAtUtc = request.ScheduledEndAt.ToUniversalTime();

        if (scheduledStartAtUtc >= scheduledEndAtUtc)
        {
            throw new BookingDomainException("ScheduledStartAt must be earlier than ScheduledEndAt.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        if (scheduledStartAtUtc <= nowUtc)
        {
            throw new BookingDomainException("A booking can only be rescheduled to a future time.");
        }

        var newDurationMinutes = (int)Math.Round((scheduledEndAtUtc - scheduledStartAtUtc).TotalMinutes);

        if (newDurationMinutes <= 0)
        {
            throw new BookingDomainException("Booking duration must be greater than zero.");
        }

        // The session length is fixed and already paid for — a reschedule keeps
        // the same duration so no price recalculation / re-payment is needed.
        if (newDurationMinutes != booking.DurationMinutes)
        {
            throw new BookingDomainException(
                "A rescheduled booking must keep the same session duration.");
        }

        // Validate the target availability slot when one is supplied.
        if (request.AvailabilityId.HasValue)
        {
            var availability = await _context.Availabilities
                .FirstOrDefaultAsync(
                    a => a.Id == request.AvailabilityId.Value
                         && a.ConsultantId == booking.ConsultantId
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
                    throw new BookingDomainException(
                        "Requested booking time is outside the selected availability range.");
                }
            }
        }

        var blockingStatuses = new[]
        {
            BookingStatus.Requested,
            BookingStatus.Confirmed
        };

        // The booking being moved must not collide with itself.
        var consultantHasConflict = await _context.Bookings.AnyAsync(
            b => b.Id != booking.Id
                 && b.ConsultantId == booking.ConsultantId
                 && blockingStatuses.Contains(b.Status)
                 && scheduledStartAtUtc < b.ScheduledEndAt
                 && scheduledEndAtUtc > b.ScheduledStartAt,
            cancellationToken);

        if (consultantHasConflict)
        {
            throw new BookingDomainException("Consultant already has a booking that overlaps this time.");
        }

        var studentHasConflict = await _context.Bookings.AnyAsync(
            b => b.Id != booking.Id
                 && b.StudentId == booking.StudentId
                 && blockingStatuses.Contains(b.Status)
                 && scheduledStartAtUtc < b.ScheduledEndAt
                 && scheduledEndAtUtc > b.ScheduledStartAt,
            cancellationToken);

        if (studentHasConflict)
        {
            throw new BookingDomainException("Student already has a booking that overlaps this time.");
        }

        booking.ScheduledStartAt = scheduledStartAtUtc;
        booking.ScheduledEndAt = scheduledEndAtUtc;
        booking.AvailabilityId = request.AvailabilityId;

        await _context.SaveChangesAsync(cancellationToken);

        await _publisher.Publish(
            new BookingRescheduledEvent(
                booking.Id,
                booking.StudentId,
                booking.ConsultantId,
                currentUserId,
                scheduledStartAtUtc,
                scheduledEndAtUtc),
            cancellationToken);
    }
}
