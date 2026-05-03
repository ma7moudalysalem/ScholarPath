using MediatR;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Commands;

public record BookmarkToggleCommand(Guid ScholarshipId) : IRequest<bool>;

public class BookmarkToggleCommandHandler(IApplicationDbContext db, ICurrentUserService user)
    : IRequestHandler<BookmarkToggleCommand, bool>
{
    public async Task<bool> Handle(BookmarkToggleCommand request, CancellationToken ct)
    {
        // N4: Unique constraint check (delete if exists, insert if not)
        var existing = await db.SavedScholarships
            .FirstOrDefaultAsync(x => x.ScholarshipId == request.ScholarshipId && x.UserId == user.UserId, ct);

        if (existing != null)
        {
            db.SavedScholarships.Remove(existing);
            await db.SaveChangesAsync(ct);
            return false; // Unbookmarked
        }

        db.SavedScholarships.Add(new SavedScholarship
        {
            UserId = user.UserId??Guid.Empty,
            ScholarshipId = request.ScholarshipId,
            SavedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(ct);
        return true; // Bookmarked
    }
}
