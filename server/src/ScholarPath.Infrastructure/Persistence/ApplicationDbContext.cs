using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Common;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence.Seeds;

namespace ScholarPath.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>, IApplicationDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService currentUserService)
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<SavedScholarship> SavedScholarships => Set<SavedScholarship>();
    public DbSet<ApplicationTracker> ApplicationTrackers => Set<ApplicationTracker>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UpgradeRequest> UpgradeRequests => Set<UpgradeRequest>();
    public DbSet<EducationEntry> EducationEntries => Set<EducationEntry>();
    public DbSet<ExpertiseTag> ExpertiseTags => Set<ExpertiseTag>();
    public DbSet<UpgradeRequestLink> UpgradeRequestLinks => Set<UpgradeRequestLink>();
    public DbSet<UpgradeRequestFile> UpgradeRequestFiles => Set<UpgradeRequestFile>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceBookmark> ResourceBookmarks => Set<ResourceBookmark>();
    public DbSet<ResourceProgress> ResourceProgresses => Set<ResourceProgress>();
    public DbSet<SuccessStory> SuccessStories => Set<SuccessStory>();

    // Phase 13 JSON to Tables
    public DbSet<ApplicationTrackerChecklistItem> ApplicationTrackerChecklistItems => Set<ApplicationTrackerChecklistItem>();
    public DbSet<ApplicationTrackerReminder> ApplicationTrackerReminders => Set<ApplicationTrackerReminder>();
    public DbSet<ScholarshipTag> ScholarshipTags => Set<ScholarshipTag>();
    public DbSet<ScholarshipEligibleCountry> ScholarshipEligibleCountries => Set<ScholarshipEligibleCountry>();
    public DbSet<ScholarshipEligibleMajor> ScholarshipEligibleMajors => Set<ScholarshipEligibleMajor>();
    public DbSet<ScholarshipDocumentChecklist> ScholarshipDocumentChecklists => Set<ScholarshipDocumentChecklist>();
    public DbSet<ResourceAttachment> ResourceAttachments => Set<ResourceAttachment>();
    public DbSet<ResourceCompletedItem> ResourceCompletedItems => Set<ResourceCompletedItem>();

    public new Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database => base.Database;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Apply all configurations from this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Default all FK delete behaviors to NoAction to prevent circular cascade paths
        foreach (var foreignKey in builder.Model.GetEntityTypes()
                     .SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;
        }

        // Seed data
        SeedData.Apply(builder);

        // Global query filters for soft-deletable entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(ApplicationDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, new object[] { builder });
            }
        }
    }

    private static void ApplySoftDeleteFilter<T>(ModelBuilder builder) where T : class, ISoftDeletable
    {
        builder.Entity<T>().HasQueryFilter(e => !e.IsDeleted);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var currentUserId = _currentUserService?.UserId?.ToString();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    break;
            }
        }

        // Handle ApplicationUser timestamps (not a BaseEntity)
        foreach (var entry in ChangeTracker.Entries<ApplicationUser>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedBy = currentUserId;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedBy = currentUserId;
                    break;
            }
        }

        // Handle soft delete
        foreach (var entry in ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = now;
                entry.Entity.DeletedBy = currentUserId;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
