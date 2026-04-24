using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Ai.Commands.LogRecommendationClick;

/// <summary>
/// Persists a <see cref="RecommendationClickEvent"/>. Ignores repeat clicks on
/// the same scholarship within a short debounce window so accidental double
/// taps don't inflate the CTR widget (PB-017 FR-249).
/// </summary>
public sealed class LogRecommendationClickCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeService clock,
    ILogger<LogRecommendationClickCommandHandler> logger)
    : IRequestHandler<LogRecommendationClickCommand, LogRecommendationClickResult?>
{
    // Same-user + same-scholarship events inside this window are treated as one click.
    private static readonly TimeSpan DebounceWindow = TimeSpan.FromMilliseconds(500);

    public async Task<LogRecommendationClickResult?> Handle(
        LogRecommendationClickCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var scholarshipExists = await db.Scholarships
            .AsNoTracking()
            .AnyAsync(s => s.Id == request.ScholarshipId, ct)
            .ConfigureAwait(false);
        if (!scholarshipExists)
        {
            throw new NotFoundException(nameof(Scholarship), request.ScholarshipId);
        }

        var now = clock.UtcNow;
        var since = now - DebounceWindow;

        var recent = await db.RecommendationClickEvents
            .AsNoTracking()
            .Where(e => e.UserId == userId
                        && e.ScholarshipId == request.ScholarshipId
                        && e.ClickedAt >= since)
            .OrderByDescending(e => e.ClickedAt)
            .Select(e => (Guid?)e.Id)
            .FirstOrDefaultAsync(ct)
            .ConfigureAwait(false);

        if (recent is Guid existingId)
        {
            logger.LogDebug(
                "Suppressed duplicate recommendation click for user {UserId} on scholarship {ScholarshipId}",
                userId, request.ScholarshipId);
            return new LogRecommendationClickResult(existingId, Deduplicated: true);
        }

        if (request.AiInteractionId is Guid aiId)
        {
            var ownsInteraction = await db.AiInteractions
                .AsNoTracking()
                .AnyAsync(i => i.Id == aiId && i.UserId == userId, ct)
                .ConfigureAwait(false);
            if (!ownsInteraction)
            {
                // Don't fail the click — just drop the correlation to keep the UI happy.
                logger.LogWarning(
                    "Dropped stale AiInteractionId {AiInteractionId} from click by {UserId}",
                    aiId, userId);
                request = request with { AiInteractionId = null };
            }
        }

        var evt = new RecommendationClickEvent
        {
            UserId = userId,
            ScholarshipId = request.ScholarshipId,
            AiInteractionId = request.AiInteractionId,
            ClickedAt = now,
            Source = string.IsNullOrWhiteSpace(request.Source) ? "card" : request.Source,
        };

        db.RecommendationClickEvents.Add(evt);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return new LogRecommendationClickResult(evt.Id, Deduplicated: false);
    }
}
