using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.ConsultantBookings.Queries.DownloadSessionRecording;

/// <summary>The recording's bytes plus the metadata needed to stream a download.</summary>
public sealed record SessionRecordingDownloadDto(Stream Content, string FileName, string ContentType);

/// <summary>
/// Streams a session recording's bytes (PB-006). Authorized to the recording's
/// booking student, its consultant, or an admin.
/// </summary>
public sealed record DownloadSessionRecordingQuery(Guid Id) : IRequest<SessionRecordingDownloadDto>;

public sealed class DownloadSessionRecordingQueryHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IBlobStorageService storage)
    : IRequestHandler<DownloadSessionRecordingQuery, SessionRecordingDownloadDto>
{
    public async Task<SessionRecordingDownloadDto> Handle(
        DownloadSessionRecordingQuery request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var recording = await db.SessionRecordings.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(SessionRecording), request.Id);

        var booking = await db.Bookings.AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == recording.BookingId, ct)
            .ConfigureAwait(false)
            ?? throw new NotFoundException(nameof(ConsultantBooking), recording.BookingId);

        var isParticipant = booking.StudentId == userId || booking.ConsultantId == userId;
        var isAdmin = currentUser.IsAdminOrSuperAdmin();
        if (!isParticipant && !isAdmin)
        {
            throw new ForbiddenAccessException("You are not allowed to download this recording.");
        }

        var content = await storage.DownloadAsync(recording.StoragePath, ct).ConfigureAwait(false);
        var fileName = $"session-recording-{recording.RecordedAt:yyyyMMdd-HHmm}.mp4";
        return new SessionRecordingDownloadDto(content, fileName, recording.ContentType);
    }
}
