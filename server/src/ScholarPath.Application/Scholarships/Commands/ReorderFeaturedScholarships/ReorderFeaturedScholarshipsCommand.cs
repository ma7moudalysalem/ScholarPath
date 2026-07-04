using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands.ReorderFeaturedScholarships;

// ─── Command ──────────────────────────────────────────────────────────────────

/// <summary>
/// Overwrites the <c>FeaturedOrder</c> of every currently-featured scholarship
/// in one atomic operation. The caller supplies all featured IDs in the desired
/// display order; the handler assigns <c>index + 1</c> to each.
///
/// All supplied IDs must currently be featured; any mismatch raises 409.
/// </summary>
public sealed record ReorderFeaturedScholarshipsCommand(
    IReadOnlyList<Guid> OrderedIds) : IRequest<Unit>;

// ─── Validator ────────────────────────────────────────────────────────────────

public sealed class ReorderFeaturedScholarshipsCommandValidator
    : AbstractValidator<ReorderFeaturedScholarshipsCommand>
{
    public ReorderFeaturedScholarshipsCommandValidator()
    {
        RuleFor(x => x.OrderedIds)
            .NotEmpty()
            .Must(ids => ids.Distinct().Count() == ids.Count)
            .WithMessage("IDs must be unique.");
    }
}

// ─── Handler ──────────────────────────────────────────────────────────────────

public sealed class ReorderFeaturedScholarshipsCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser)
    : IRequestHandler<ReorderFeaturedScholarshipsCommand, Unit>
{
    public async Task<Unit> Handle(
        ReorderFeaturedScholarshipsCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAdminOrSuperAdmin())
            throw new ForbiddenAccessException("Only an administrator can reorder featured scholarships.");

        // Load the full current featured set in one round-trip.
        var featured = await db.Scholarships
            .Where(s => s.IsFeatured && !s.IsDeleted)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var featuredIds = featured.Select(s => s.Id).ToHashSet();

        // Validate: every submitted ID must actually be featured.
        var unknown = request.OrderedIds.Where(id => !featuredIds.Contains(id)).ToList();
        if (unknown.Count != 0)
            throw new ConflictException(
                $"The following scholarship IDs are not currently featured: {string.Join(", ", unknown)}.");

        // Validate: no currently-featured scholarship is absent from the list.
        var missing = featuredIds.Where(id => !request.OrderedIds.Contains(id)).ToList();
        if (missing.Count != 0)
            throw new ConflictException(
                $"The following featured scholarships are missing from the reorder list: {string.Join(", ", missing)}.");

        // Apply the new order.
        var indexMap = request.OrderedIds
            .Select((id, i) => (id, order: i + 1))
            .ToDictionary(x => x.id, x => x.order);

        foreach (var s in featured)
            s.FeaturedOrder = indexMap[s.Id];

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        return Unit.Value;
    }
}
