using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace ScholarPath.Application.Scholarships.Commands;
public record ArchiveScholarshipCommand(Guid Id) : IRequest<bool>;

// Handler
public class ArchiveScholarshipCommandHandler(
    IApplicationDbContext db,
    ICurrentUserService user) : IRequestHandler<ArchiveScholarshipCommand, bool>
{
    public async Task<bool> Handle(ArchiveScholarshipCommand request, CancellationToken ct)
    {
        var entity = await db.Scholarships
            .FirstOrDefaultAsync(x => x.Id == request.Id, ct);

        if (entity == null)
        {
            throw new NotFoundException(nameof(Scholarship), request.Id);
        }

        // Only the owning company (or an admin) may archive a listing.
        if (entity.OwnerCompanyId != user.UserId && !user.IsInRole("Admin"))
        {
            throw new ForbiddenAccessException("You can only archive your own scholarship listings.");
        }

        // Soft delete
        entity.IsDeleted = true;

        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = user.UserId;
        entity.ArchivedAt = DateTimeOffset.UtcNow;
        entity.Status = ScholarshipStatus.Archived;

        await db.SaveChangesAsync(ct);

        return true;
    }
}
