using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Resources.Commands.ToggleResourceBookmark;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>Toggles the caller's bookmark on a resource (PB-009 AC#5). Returns the new state.</summary>
public sealed record ToggleResourceBookmarkCommand(Guid ResourceId) : IRequest<bool>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ToggleResourceBookmarkCommandValidator
    : AbstractValidator<ToggleResourceBookmarkCommand>
{
    public ToggleResourceBookmarkCommandValidator() => RuleFor(x => x.ResourceId).NotEmpty();
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ToggleResourceBookmarkCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ToggleResourceBookmarkCommand, bool>
{
    public async Task<bool> Handle(ToggleResourceBookmarkCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId
            ?? throw new ForbiddenAccessException("Not authenticated.");

        var exists = await db.Resources.AnyAsync(r => r.Id == request.ResourceId, ct);
        if (!exists)
            throw new NotFoundException(nameof(Resource), request.ResourceId);

        var bookmark = await db.ResourceBookmarks
            .FirstOrDefaultAsync(b => b.UserId == userId && b.ResourceId == request.ResourceId, ct);

        if (bookmark is not null)
        {
            db.ResourceBookmarks.Remove(bookmark);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            return false;
        }

        db.ResourceBookmarks.Add(new ResourceBookmark
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ResourceId = request.ResourceId,
            BookmarkedAt = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
