using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Application abstraction over the EF DbContext. Infrastructure implements this.
/// Commands/queries use this rather than concrete DbContext for testability.
/// </summary>
public interface IApplicationDbContext
{
    /// <summary>
    /// Provides access to database-level operations such as <c>SqlQuery&lt;T&gt;</c>
    /// for raw SQL projections against SQL views and stored procedures.
    /// </summary>
    DatabaseFacade Database { get; }
    DbSet<ApplicationUser> Users { get; }
    DbSet<UserProfile> UserProfiles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<PasswordResetToken> PasswordResetTokens { get; }
    DbSet<LoginAttempt> LoginAttempts { get; }
    DbSet<UpgradeRequest> UpgradeRequests { get; }
    DbSet<UpgradeRequestFile> UpgradeRequestFiles { get; }
    DbSet<UpgradeRequestLink> UpgradeRequestLinks { get; }
    DbSet<EducationEntry> EducationEntries { get; }
    DbSet<ExpertiseTag> ExpertiseTags { get; }

    DbSet<Category> Categories { get; }
    DbSet<Scholarship> Scholarships { get; }
    DbSet<ScholarshipChild> ScholarshipChildren { get; }
    DbSet<SavedScholarship> SavedScholarships { get; }

    DbSet<ApplicationTracker> Applications { get; }
    DbSet<ApplicationTrackerChild> ApplicationChildren { get; }

    DbSet<CompanyReview> CompanyReviews { get; }
    DbSet<CompanyReviewPayment> CompanyReviewPayments { get; }
    DbSet<ConsultantReview> ConsultantReviews { get; }

    DbSet<ConsultantAvailability> Availabilities { get; }
    DbSet<ConsultantBooking> Bookings { get; }

    DbSet<Payment> Payments { get; }
    DbSet<Payout> Payouts { get; }
    DbSet<StripeWebhookEvent> StripeWebhookEvents { get; }
    DbSet<ProfitShareConfig> ProfitShareConfigs { get; }
    DbSet<FinancialConfigRule> FinancialConfigRules { get; }

    DbSet<ForumCategory> ForumCategories { get; }
    DbSet<ForumPost> ForumPosts { get; }
    DbSet<ForumPostAttachment> ForumPostAttachments { get; }
    DbSet<ForumVote> ForumVotes { get; }
    DbSet<ForumFlag> ForumFlags { get; }

    DbSet<ChatConversation> Conversations { get; }
    DbSet<ChatMessage> Messages { get; }
    DbSet<UserBlock> UserBlocks { get; }

    DbSet<AiInteraction> AiInteractions { get; }
    DbSet<RecommendationClickEvent> RecommendationClickEvents { get; }
    DbSet<AiRedactionAuditSample> AiRedactionAuditSamples { get; }
    DbSet<KnowledgeDocument> KnowledgeDocuments { get; }

    DbSet<Resource> Resources { get; }
    DbSet<ResourceChild> ResourceChapters { get; }
    DbSet<ResourceBookmark> ResourceBookmarks { get; }
    DbSet<ResourceProgress> ResourceProgress { get; }
    DbSet<ResourceProgressChild> ResourceProgressChildren { get; }

    DbSet<Document> Documents { get; }

    DbSet<SessionRecording> SessionRecordings { get; }

    DbSet<Notification> Notifications { get; }
    DbSet<NotificationPreference> NotificationPreferences { get; }

    DbSet<AuditLog> AuditLogs { get; }
    DbSet<UserDataRequest> UserDataRequests { get; }
    DbSet<SuccessStory> SuccessStories { get; }
    DbSet<UserRiskFlag> UserRiskFlags { get; }
    DbSet<PlatformSetting> PlatformSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
