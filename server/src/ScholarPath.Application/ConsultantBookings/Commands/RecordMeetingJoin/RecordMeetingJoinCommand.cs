using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;

namespace ScholarPath.Application.ConsultantBookings.Commands.RecordMeetingJoin;

/// <summary>
/// Records that the authenticated participant (the booking's student or
/// consultant) has joined the session room. The first join per party is kept —
/// the no-show sweep job (FR-217) reads these timestamps to attribute an
/// automated no-show to whichever party never joined.
/// </summary>
public sealed record RecordMeetingJoinCommand(Guid BookingId) : IRequest<MeetingJoinResult>;

/// <summary>What the client needs to enter the session room.</summary>
public sealed record MeetingJoinResult(Guid BookingId, string? MeetingUrl, DateTimeOffset JoinedAt);

public sealed class RecordMeetingJoinCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser)
    : IRequestHandler<RecordMeetingJoinCommand, MeetingJoinResult>
{
    // A participant may enter the room from 15 minutes before the start until
    // 15 minutes after the scheduled end — wide enough for early/late arrivals,
    // tight enough that a join cannot be faked long after the session ended.
    private static readonly TimeSpan JoinOpensBefore = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan JoinClosesAfter = TimeSpan.FromMinutes(15);

    public async Task<MeetingJoinResult> Handle(RecordMeetingJoinCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var userId = currentUser.UserId
            ?? throw new UnauthorizedAccessException("Authenticated user id is missing.");

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct)
            .ConfigureAwait(false)
            ?? throw new BookingDomainException("Booking was not found.");

        var isStudent = booking.StudentId == userId;
        var isConsultant = booking.ConsultantId == userId;
        if (!isStudent && !isConsultant)
        {
            throw new UnauthorizedAccessException("You are not a participant of this booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only a confirmed booking has a session room.");
        }

        var nowUtc = DateTimeOffset.UtcNow;
        var opensAt = booking.ScheduledStartAt.ToUniversalTime() - JoinOpensBefore;
        var closesAt = booking.ScheduledEndAt.ToUniversalTime() + JoinClosesAfter;

        if (nowUtc < opensAt)
        {
            throw new BookingDomainException("The session room is not open yet.");
        }
        if (nowUtc > closesAt)
        {
            throw new BookingDomainException("The session room has closed.");
        }

        // First join per party wins — a re-join must not overwrite the
        // attendance proof the no-show sweep relies on.
        if (isStudent && booking.StudentJoinedAt is null)
        {
            booking.StudentJoinedAt = nowUtc;
        }
        else if (isConsultant && booking.ConsultantJoinedAt is null)
        {
            booking.ConsultantJoinedAt = nowUtc;
        }

        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        var joinedAt = isStudent ? booking.StudentJoinedAt!.Value : booking.ConsultantJoinedAt!.Value;
        return new MeetingJoinResult(booking.Id, booking.MeetingUrl, joinedAt);
    }
}
