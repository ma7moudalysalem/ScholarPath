using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.GetBookingRecordings;

/// <summary>A session-recording entry as exposed on the wire — never carries the bytes.</summary>
public sealed record SessionRecordingDto(
    Guid Id,
    Guid BookingId,
    DateTimeOffset RecordedAt,
    long SizeBytes,
    string ContentType);

/// <summary>
/// Lists a booking's session recordings (PB-006). Authorized to the booking's
/// student, its consultant, or an admin.
/// </summary>
public sealed record GetBookingRecordingsQuery(Guid BookingId)
    : IRequest<IReadOnlyList<SessionRecordingDto>>;

public sealed class GetBookingRecordingsQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<GetBookingRecordingsQuery, IReadOnlyList<SessionRecordingDto>>
{
    public async Task<IReadOnlyList<SessionRecordingDto>> Handle(
        GetBookingRecordingsQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var booking = await db.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ConsultantBooking), request.BookingId);

        var isParticipant = booking.StudentId == userId || booking.ConsultantId == userId;
        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        if (!isParticipant && !isAdmin)
        {
            throw new ForbiddenAccessException(
                "You are not allowed to view this booking's recordings.");
        }

        var recordings = await db.SessionRecordings.AsNoTracking()
            .Where(r => r.BookingId == request.BookingId)
            .OrderByDescending(r => r.RecordedAt)
            .Select(r => new SessionRecordingDto(
                r.Id, r.BookingId, r.RecordedAt, r.SizeBytes, r.ContentType))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return recordings;
    }
}
