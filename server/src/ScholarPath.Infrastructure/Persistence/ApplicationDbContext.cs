using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Common;
using ScholarPath.Domain.Entities;
using ScholarPath.Infrastructure.Persistence.Configurations;
using System.Reflection;

namespace ScholarPath.Infrastructure.Persistence;

/// <param name="encryption">
/// Application-level field-encryption service. Optional: when supplied (the
/// running app) <see cref="OnModelCreating"/> wires the AES-256-GCM
/// <see cref="EncryptedStringConverter"/> onto the sensitive columns. When
/// <see langword="null"/> (EF design-time tooling, in-memory unit-test contexts)
/// the columns are mapped as plain strings — migrations only ever change column
/// width, never the data, so a design-time context needs no key.
/// </param>
public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMediator? mediator = null,
    IFieldEncryptionService? encryption = null)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options),
      IApplicationDbContext
{
    /// <summary>
    /// True when an <see cref="IFieldEncryptionService"/> was injected, so
    /// <see cref="OnModelCreating"/> wires the field-encryption value converter.
    /// Read by <see cref="EncryptionAwareModelCacheKeyFactory"/> to keep the
    /// encryption-on and encryption-off models in separate cache slots.
    /// </summary>
    internal bool FieldEncryptionEnabled => encryption is not null;

    // Users + onboarding
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<UpgradeRequest> UpgradeRequests => Set<UpgradeRequest>();
    public DbSet<UpgradeRequestFile> UpgradeRequestFiles => Set<UpgradeRequestFile>();
    public DbSet<UpgradeRequestLink> UpgradeRequestLinks => Set<UpgradeRequestLink>();
    public DbSet<EducationEntry> EducationEntries => Set<EducationEntry>();
    public DbSet<ExpertiseTag> ExpertiseTags => Set<ExpertiseTag>();

    DbSet<ApplicationUser> IApplicationDbContext.Users => Set<ApplicationUser>();

    // Scholarships
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Scholarship> Scholarships => Set<Scholarship>();
    public DbSet<ScholarshipChild> ScholarshipChildren => Set<ScholarshipChild>();
    public DbSet<SavedScholarship> SavedScholarships => Set<SavedScholarship>();

    // Applications
    public DbSet<ApplicationTracker> Applications => Set<ApplicationTracker>();
    public DbSet<ApplicationTrackerChild> ApplicationChildren => Set<ApplicationTrackerChild>();

    // Reviews
    public DbSet<CompanyReview> CompanyReviews => Set<CompanyReview>();
    public DbSet<CompanyReviewPayment> CompanyReviewPayments => Set<CompanyReviewPayment>();
    public DbSet<ConsultantReview> ConsultantReviews => Set<ConsultantReview>();

    // Bookings
    public DbSet<ConsultantAvailability> Availabilities => Set<ConsultantAvailability>();
    public DbSet<ConsultantBooking> Bookings => Set<ConsultantBooking>();

    // Payments
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<StripeWebhookEvent> StripeWebhookEvents => Set<StripeWebhookEvent>();
    public DbSet<ProfitShareConfig> ProfitShareConfigs => Set<ProfitShareConfig>();
    public DbSet<FinancialConfigRule> FinancialConfigRules => Set<FinancialConfigRule>();

    // Community
    public DbSet<ForumCategory> ForumCategories => Set<ForumCategory>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumPostAttachment> ForumPostAttachments => Set<ForumPostAttachment>();
    public DbSet<ForumVote> ForumVotes => Set<ForumVote>();
    public DbSet<ForumFlag> ForumFlags => Set<ForumFlag>();
    public DbSet<ForumBookmark> ForumBookmarks => Set<ForumBookmark>();
    public DbSet<ForumTag> ForumTags => Set<ForumTag>();
    public DbSet<ForumPostTag> ForumPostTags => Set<ForumPostTag>();

    // Chat
    public DbSet<ChatConversation> Conversations => Set<ChatConversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    // AI
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();
    public DbSet<RecommendationClickEvent> RecommendationClickEvents => Set<RecommendationClickEvent>();
    public DbSet<AiRedactionAuditSample> AiRedactionAuditSamples => Set<AiRedactionAuditSample>();
    public DbSet<KnowledgeDocument> KnowledgeDocuments => Set<KnowledgeDocument>();

    // Resources
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceChild> ResourceChapters => Set<ResourceChild>();
    public DbSet<ResourceBookmark> ResourceBookmarks => Set<ResourceBookmark>();
    public DbSet<ResourceProgress> ResourceProgress => Set<ResourceProgress>();
    public DbSet<ResourceProgressChild> ResourceProgressChildren => Set<ResourceProgressChild>();

    // Document vault (FR-216)
    public DbSet<Document> Documents => Set<Document>();

    // Consultant-session recordings (PB-006)
    public DbSet<SessionRecording> SessionRecordings => Set<SessionRecording>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Cross-cutting
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserDataRequest> UserDataRequests => Set<UserDataRequest>();
    public DbSet<SuccessStory> SuccessStories => Set<SuccessStory>();
    public DbSet<UserRiskFlag> UserRiskFlags => Set<UserRiskFlag>();
    public DbSet<PlatformSetting> PlatformSettings => Set<PlatformSetting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // ApplicationDbContext's model varies at runtime — the field-encryption
        // converter is only applied when an IFieldEncryptionService was injected.
        // Swap in a cache-key factory that accounts for that, so an
        // encryption-aware context and a plain one never share a cached model.
        optionsBuilder.ReplaceService<IModelCacheKeyFactory, EncryptionAwareModelCacheKeyFactory>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(builder);

        // Ignore domain-events collections globally — they're a DDD concern, not persisted
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var domainEventsProp = entityType.ClrType.GetProperty("DomainEvents");
            if (domainEventsProp is not null)
            {
                builder.Entity(entityType.ClrType).Ignore(nameof(BaseEntity.DomainEvents));
            }
        }

        // Rename Identity tables to cleaner names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<Guid>>().ToTable("UserTokens");
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<Guid>>().ToTable("RoleClaims");

        // Application-level encryption of sensitive columns at rest (security
        // NFR): wire the AES-256-GCM ValueConverter onto the chosen PII columns.
        // The column list lives in EntityConfigurations so it sits next to the
        // rest of the mapping. Skipped when no encryption service is available
        // (design-time tooling / in-memory test contexts) — those never persist
        // production data.
        if (encryption is not null)
        {
            builder.ApplyFieldEncryption(encryption);
        }

        // SQL Server refuses multiple CASCADE paths to the same table — which
        // any entity with 2+ FKs to ApplicationUser triggers. Rather than
        // adding explicit Restrict clauses in every Configuration, we sweep
        // every FK pointing at ApplicationUser and force DeleteBehavior.Restrict
        // globally. Individual configurations can still override this for
        // entities that truly want Cascade.
        foreach (var fk in builder.Model.GetEntityTypes()
            .SelectMany(e => e.GetForeignKeys())
            .Where(fk => fk.PrincipalEntityType.ClrType == typeof(ApplicationUser)
                      && fk.DeleteBehavior == DeleteBehavior.Cascade))
        {
            fk.DeleteBehavior = DeleteBehavior.Restrict;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Stamp audit timestamps before persisting: CreatedAt on new rows (only
        // when not already set, so seeders keep their intentional back-dated
        // values) and UpdatedAt on every modification. Without this, entities
        // created through commands (forum posts/replies, etc.) would persist
        // with CreatedAt = default (0001-01-01), breaking date display and any
        // ordering by creation time.
        var auditNow = DateTimeOffset.UtcNow;
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedAt == default)
            {
                entry.Entity.CreatedAt = auditNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = auditNow;
            }
        }

        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            // Publish domain events only after the changes are durably persisted.
            await DispatchDomainEventsAsync(cancellationToken);
            return result;
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx
                                           && (sqlEx.Number == 2601 || sqlEx.Number == 2627))
        {
            // Translate a unique-index violation into a domain-level conflict the API
            // understands, with a message tailored to the index that was hit.
            var detail = sqlEx.Message;
            var message =
                detail.Contains("UX_Bookings_Consultant_Slot_Active", StringComparison.OrdinalIgnoreCase)
                    ? "That consultation slot has just been booked by someone else."
                : detail.Contains("UX_Applications_Student_Scholarship_Active", StringComparison.OrdinalIgnoreCase)
                    ? "An active application already exists for this scholarship."
                : "This action conflicts with an existing record.";
            throw new ConflictException(message);
        }
    }
    

    private async Task DispatchDomainEventsAsync(CancellationToken ct)
    {
        if (mediator is null) return;

        var entities = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var userEntities = ChangeTracker.Entries<ApplicationUser>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = entities.SelectMany(e => e.DomainEvents)
            .Concat(userEntities.SelectMany(u => u.DomainEvents))
            .ToList();

        entities.ForEach(e => e.ClearDomainEvents());
        userEntities.ForEach(u => u.ClearDomainEvents());

        foreach (var @event in events)
        {
            await mediator.Publish(@event, ct).ConfigureAwait(false);
        }
    }
}
