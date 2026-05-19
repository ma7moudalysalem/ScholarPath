using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Exceptions;

namespace ScholarPath.Application.ConsultantBookings.Commands.StartMeetingRecording;

/// <summary>
/// Starts recording a confirmed booking's video session (PB-006). The client
/// reads <paramref name="ServerCallId"/> from the live ACS call. Idempotent —
/// the first participant to reach a connected call starts the recording; any
/// later call is a no-op.
/// </summary>
public sealed record StartMeetingRecordingCommand(Guid BookingId, string ServerCallId) : IRequest;

public sealed class StartMeetingRecordingCommandHandler(
    IApplicationDbContext context,
    ICurrentUserService currentUser,
    IMeetingService meetingService)
    : IRequestHandler<StartMeetingRecordingCommand>
{
    public async Task Handle(StartMeetingRecordingCommand request, CancellationToken ct)
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

        if (booking.StudentId != userId && booking.ConsultantId != userId)
        {
            throw new UnauthorizedAccessException("You are not a participant of this booking.");
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            throw new BookingDomainException("Only a confirmed booking's session can be recorded.");
        }

        // Already recording — the other participant (or an earlier call) started it.
        if (booking.RecordingStartedAt is not null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(request.ServerCallId))
        {
            throw new BookingDomainException("A server call id is required to start recording.");
        }

        var recordingId = await meetingService
            .StartRecordingAsync(request.ServerCallId, ct)
            .ConfigureAwait(false);

        booking.RecordingStartedAt = DateTimeOffset.UtcNow;
        booking.RecordingId = recordingId;

        try
        {
            await context.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            // The other participant's client started the recording at the same
            // moment and won the write — nothing more to do.
        }
    }
}
