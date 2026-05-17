using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Commands.CompleteResourceChapter;

// ─── Result + Command ─────────────────────────────────────────────────────────

public sealed record ChapterProgressResult(
    Guid ResourceId,
    int ChaptersCompletedCount,
    int TotalChapters);

/// <summary>Marks one chapter complete for the caller and refreshes their progress (PB-009 AC#6).</summary>
public sealed record CompleteResourceChapterCommand(
    Guid ResourceId, Guid ChapterId) : IRequest<ChapterProgressResult>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class CompleteResourceChapterCommandValidator
    : AbstractValidator<CompleteResourceChapterCommand>
{
    public CompleteResourceChapterCommandValidator()
    {
        RuleFor(x => x.ResourceId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class CompleteResourceChapterCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<CompleteResourceChapterCommand, ChapterProgressResult>
{
    public async Task<ChapterProgressResult> Handle(
        CompleteResourceChapterCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        // The chapter must really belong to the resource.
        var chapterExists = await db.ResourceChapters.AnyAsync(
            c => c.Id == request.ChapterId && c.ResourceId == request.ResourceId, ct);
        if (!chapterExists)
            throw new NotFoundException(nameof(ResourceChild), request.ChapterId);

        var now = DateTimeOffset.UtcNow;

        var progress = await db.ResourceProgress
            .Include(p => p.ChapterProgress)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ResourceId == request.ResourceId, ct);

        if (progress is null)
        {
            progress = new ResourceProgress
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ResourceId = request.ResourceId,
                LastAccessedAt = now,
            };
            db.ResourceProgress.Add(progress);
        }

        var chapterProgress = progress.ChapterProgress
            .FirstOrDefault(cp => cp.ResourceChildId == request.ChapterId);

        if (chapterProgress is null)
        {
            progress.ChapterProgress.Add(new ResourceProgressChild
            {
                Id = Guid.NewGuid(),
                ResourceProgressId = progress.Id,
                ResourceChildId = request.ChapterId,
                IsCompleted = true,
                CompletedAt = now,
            });
        }
        else if (!chapterProgress.IsCompleted)
        {
            chapterProgress.IsCompleted = true;
            chapterProgress.CompletedAt = now;
        }

        progress.ChaptersCompletedCount = progress.ChapterProgress.Count(cp => cp.IsCompleted);
        progress.LastAccessedAt = now;

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var totalChapters = await db.ResourceChapters
            .CountAsync(c => c.ResourceId == request.ResourceId, ct);

        return new ChapterProgressResult(
            request.ResourceId, progress.ChaptersCompletedCount, totalChapters);
    }
}
