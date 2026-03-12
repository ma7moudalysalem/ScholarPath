using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Scholarship> Scholarships { get; }
    DbSet<SavedScholarship> SavedScholarships { get; }
    DbSet<ApplicationTracker> ApplicationTrackers { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<Resource> Resources { get; }
    DbSet<ResourceBookmark> ResourceBookmarks { get; }
    DbSet<ResourceProgress> ResourceProgresses { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<UpgradeRequest> UpgradeRequests { get; }
    DbSet<UpgradeRequestFile> UpgradeRequestFiles { get; }
    DbSet<UpgradeRequestLink> UpgradeRequestLinks { get; }
    DbSet<ExpertiseTag> ExpertiseTags { get; }
    DbSet<EducationEntry> EducationEntries { get; }
    DbSet<Notification> Notifications { get; }
    
    // Phase 13 JSON to Tables
    DbSet<ApplicationTrackerChecklistItem> ApplicationTrackerChecklistItems { get; }
    DbSet<ApplicationTrackerReminder> ApplicationTrackerReminders { get; }
    DbSet<ScholarshipTag> ScholarshipTags { get; }
    DbSet<ScholarshipEligibleCountry> ScholarshipEligibleCountries { get; }
    DbSet<ScholarshipEligibleMajor> ScholarshipEligibleMajors { get; }
    DbSet<ScholarshipDocumentChecklist> ScholarshipDocumentChecklists { get; }
    DbSet<ResourceAttachment> ResourceAttachments { get; }
    DbSet<ResourceCompletedItem> ResourceCompletedItems { get; }

    Microsoft.EntityFrameworkCore.Infrastructure.DatabaseFacade Database { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
