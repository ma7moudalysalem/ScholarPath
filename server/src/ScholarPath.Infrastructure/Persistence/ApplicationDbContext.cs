using System.Reflection;
using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Common;
using ScholarPath.Domain.Entities;

namespace ScholarPath.Infrastructure.Persistence;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options,
    IMediator? mediator = null)
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options),
      IApplicationDbContext
{
    // Users + onboarding
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
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

    // Community
    public DbSet<ForumCategory> ForumCategories => Set<ForumCategory>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<ForumPostAttachment> ForumPostAttachments => Set<ForumPostAttachment>();
    public DbSet<ForumVote> ForumVotes => Set<ForumVote>();
    public DbSet<ForumFlag> ForumFlags => Set<ForumFlag>();

    // Chat
    public DbSet<ChatConversation> Conversations => Set<ChatConversation>();
    public DbSet<ChatMessage> Messages => Set<ChatMessage>();
    public DbSet<UserBlock> UserBlocks => Set<UserBlock>();

    // AI
    public DbSet<AiInteraction> AiInteractions => Set<AiInteraction>();
    public DbSet<RecommendationClickEvent> RecommendationClickEvents => Set<RecommendationClickEvent>();
    public DbSet<AiRedactionAuditSample> AiRedactionAuditSamples => Set<AiRedactionAuditSample>();

    // Resources
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<ResourceChild> ResourceChapters => Set<ResourceChild>();
    public DbSet<ResourceBookmark> ResourceBookmarks => Set<ResourceBookmark>();
    public DbSet<ResourceProgress> ResourceProgress => Set<ResourceProgress>();
    public DbSet<ResourceProgressChild> ResourceProgressChildren => Set<ResourceProgressChild>();

    // Notifications
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // Cross-cutting
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserDataRequest> UserDataRequests => Set<UserDataRequest>();
    public DbSet<SuccessStory> SuccessStories => Set<SuccessStory>();
    public DbSet<UserRiskFlag> UserRiskFlags => Set<UserRiskFlag>();

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
        var result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await DispatchDomainEventsAsync(cancellationToken).ConfigureAwait(false);
        return result;
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
