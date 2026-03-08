using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Scholarship> Scholarships { get; }
    DbSet<SavedScholarship> SavedScholarships { get; }
    DbSet<UserProfile> UserProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
