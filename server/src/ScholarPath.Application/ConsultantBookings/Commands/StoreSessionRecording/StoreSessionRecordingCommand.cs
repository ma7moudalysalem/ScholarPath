using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.ConsultantBookings.Commands.StoreSessionRecording;

/// <summary>
/// Moves a finished ACS recording into blob storage and records its metadata
/// (PB-006). Raised by the recording-ready Event Grid webhook. Idempotent —
/// Event Grid can redeliver the same event.
/// </summary>
public sealed record StoreSessionRecordingCommand(string RecordingId, string ContentLocation) : IRequest;

public sealed class StoreSessionRecordingCommandHandler(
    IApplicationDbContext context,
    IMeetingService meetingService,
    IBlobStorageService storage,
    ILogger<StoreSessionRecordingCommandHandler> logger)
    : IRequestHandler<StoreSessionRecordingCommand>
{
    private const string Container = "session-recordings";

    public async Task Handle(StoreSessionRecordingCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.RecordingId)
            || string.IsNullOrWhiteSpace(request.ContentLocation))
        {
            return;
        }

        // Event Grid delivers at-least-once — store each recording exactly once.
        var alreadyStored = await context.SessionRecordings
            .IgnoreQueryFilters()
            .AnyAsync(r => r.RecordingId == request.RecordingId, ct)
            .ConfigureAwait(false);
        if (alreadyStored)
        {
            return;
        }

        var booking = await context.Bookings
            .FirstOrDefaultAsync(b => b.RecordingId == request.RecordingId, ct)
            .ConfigureAwait(false);
        if (booking is null)
        {
            logger.LogWarning(
                "Recording {RecordingId} has no matching booking; skipping storage.",
                request.RecordingId);
            return;
        }

        await using var source = await meetingService
            .DownloadRecordingAsync(request.ContentLocation, ct)
            .ConfigureAwait(false);
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer, ct).ConfigureAwait(false);
        buffer.Position = 0;

        var fileName = $"session-{booking.Id:N}-{request.RecordingId}.mp4";
        var storagePath = await storage
            .UploadAsync(buffer, fileName, "video/mp4", Container, ct)
            .ConfigureAwait(false);

        context.SessionRecordings.Add(new SessionRecording
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            RecordingId = request.RecordingId,
            StoragePath = storagePath,
            ContentType = "video/mp4",
            SizeBytes = buffer.Length,
            RecordedAt = booking.RecordingStartedAt ?? DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Stored session recording {RecordingId} for booking {BookingId} ({Bytes} bytes).",
            request.RecordingId, booking.Id, buffer.Length);
    }
}
