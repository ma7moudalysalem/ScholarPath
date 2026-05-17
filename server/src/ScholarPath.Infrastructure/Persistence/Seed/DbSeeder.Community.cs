using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the community forum: <see cref="ForumCategory"/> rows, root
    /// <see cref="ForumPost"/>s covering EVERY <see cref="PostModerationStatus"/>
    /// (Visible / Hidden / Removed / PendingReview), threaded replies,
    /// <see cref="ForumVote"/>s (up and down), and <see cref="ForumFlag"/>s.
    /// Cached aggregate counters on the posts are set to match the seeded
    /// votes / replies / flags. Idempotent on <see cref="ForumPost"/> being empty.
    /// </summary>
    private static async Task SeedCommunityAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.ForumPosts.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // --- categories --------------------------------------------------
        var catApplications = new ForumCategory { NameEn = "Applications & Essays", NameAr = "الطلبات والمقالات", Slug = "applications-essays", DescriptionEn = "Help with scholarship applications and personal statements.", DescriptionAr = "المساعدة في طلبات المنح والبيانات الشخصية.", DisplayOrder = 1, IsActive = true, CreatedAt = now.AddDays(-90) };
        var catFunding = new ForumCategory { NameEn = "Funding & Finance", NameAr = "التمويل والمالية", Slug = "funding-finance", DescriptionEn = "Discuss funding options, stipends and budgeting abroad.", DescriptionAr = "مناقشة خيارات التمويل والرواتب والميزانية في الخارج.", DisplayOrder = 2, IsActive = true, CreatedAt = now.AddDays(-90) };
        var catLife = new ForumCategory { NameEn = "Student Life Abroad", NameAr = "حياة الطالب في الخارج", Slug = "student-life-abroad", DescriptionEn = "Housing, visas and settling into a new country.", DescriptionAr = "السكن والتأشيرات والاستقرار في بلد جديد.", DisplayOrder = 3, IsActive = true, CreatedAt = now.AddDays(-90) };

        db.ForumCategories.AddRange(catApplications, catFunding, catLife);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- root posts: one per moderation status -----------------------
        // Visible — a normal, healthy thread with replies and votes.
        var visiblePost = new ForumPost
        {
            AuthorId = users.Students[0].Id,
            CategoryId = catApplications.Id,
            Title = "How long should a statement of purpose be?",
            BodyMarkdown = "I'm applying for a Master's scholarship and the guidance only says \"concise\". Is 800 words too long?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-14),
        };
        // Visible #2 — in the funding category.
        var visiblePost2 = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catFunding.Id,
            Title = "Fully funded vs partially funded — is the difference worth it?",
            BodyMarkdown = "I have an offer for a partially funded scholarship but I'm waiting on a fully funded one. Should I hold out?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-9),
        };
        // PendingReview — newly posted, awaiting moderation.
        var pendingPost = new ForumPost
        {
            AuthorId = users.Students[3].Id,
            CategoryId = catLife.Id,
            Title = "Cheapest student housing near campus?",
            BodyMarkdown = "Looking for tips on affordable housing. This post is awaiting moderator review.",
            ModerationStatus = PostModerationStatus.PendingReview,
            CreatedAt = now.AddHours(-5),
        };
        // Hidden — auto-hidden after crossing the flag threshold.
        var hiddenPost = new ForumPost
        {
            AuthorId = users.Students[4].Id,
            CategoryId = catApplications.Id,
            Title = "Selling essay templates — DM me",
            BodyMarkdown = "This post was auto-hidden after community members flagged it.",
            ModerationStatus = PostModerationStatus.Hidden,
            IsAutoHidden = true,
            AutoHiddenAt = now.AddDays(-2),
            CreatedAt = now.AddDays(-3),
        };
        // Removed — an admin took it down.
        var removedPost = new ForumPost
        {
            AuthorId = users.Students[4].Id,
            CategoryId = catFunding.Id,
            Title = "Removed: off-topic advertisement",
            BodyMarkdown = "This post violated the community guidelines and was removed by an administrator.",
            ModerationStatus = PostModerationStatus.Removed,
            ModeratedByAdminId = users.PrimaryAdmin.Id,
            ModeratedAt = now.AddDays(-1),
            ModerationNote = "Spam / advertising. Removed per community guidelines.",
            CreatedAt = now.AddDays(-4),
        };

        var roots = new[] { visiblePost, visiblePost2, pendingPost, hiddenPost, removedPost };
        db.ForumPosts.AddRange(roots);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- replies (non-null ParentPostId) -----------------------------
        var reply1 = new ForumPost
        {
            AuthorId = users.Consultants[0].Id,
            CategoryId = catApplications.Id,
            ParentPostId = visiblePost.Id,
            BodyMarkdown = "800 words is fine if every sentence earns its place. Most committees expect 700–900.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-13),
        };
        var reply2 = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catApplications.Id,
            ParentPostId = visiblePost.Id,
            BodyMarkdown = "Agreed — I trimmed mine from 1,100 to 850 and it read much better.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-12),
        };
        var reply3 = new ForumPost
        {
            AuthorId = users.Consultants[1].Id,
            CategoryId = catFunding.Id,
            ParentPostId = visiblePost2.Id,
            BodyMarkdown = "If the fully funded offer covers living costs, the difference is usually worth a short wait.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-8),
        };

        var replies = new[] { reply1, reply2, reply3 };
        db.ForumPosts.AddRange(replies);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- votes (unique per (post, user)) -----------------------------
        var votes = new List<ForumVote>
        {
            new() { ForumPostId = visiblePost.Id, UserId = users.Students[1].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-13) },
            new() { ForumPostId = visiblePost.Id, UserId = users.Students[2].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = visiblePost.Id, UserId = users.Consultants[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = visiblePost2.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-8) },
            new() { ForumPostId = visiblePost2.Id, UserId = users.Students[3].Id, VoteType = VoteType.Down, VotedAt = now.AddDays(-7) },
            new() { ForumPostId = reply1.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = hiddenPost.Id, UserId = users.Students[1].Id, VoteType = VoteType.Down, VotedAt = now.AddDays(-3) },
        };
        db.ForumVotes.AddRange(votes);

        // --- flags (unique per (post, flagger)) --------------------------
        var flags = new List<ForumFlag>
        {
            new() { ForumPostId = hiddenPost.Id, FlaggedByUserId = users.Students[0].Id, Reason = "Spam", AdditionalDetails = "Selling essay templates.", FlaggedAt = now.AddDays(-3), IsValid = true },
            new() { ForumPostId = hiddenPost.Id, FlaggedByUserId = users.Students[1].Id, Reason = "Spam", FlaggedAt = now.AddDays(-3), IsValid = true },
            new() { ForumPostId = hiddenPost.Id, FlaggedByUserId = users.Students[2].Id, Reason = "Academic dishonesty", FlaggedAt = now.AddDays(-2), IsValid = true },
            new() { ForumPostId = removedPost.Id, FlaggedByUserId = users.Students[1].Id, Reason = "Advertising", FlaggedAt = now.AddDays(-2), IsValid = true },
        };
        db.ForumFlags.AddRange(flags);

        // --- sync cached aggregate counters on the posts -----------------
        foreach (var post in roots.Concat(replies))
        {
            post.UpvoteCount = votes.Count(v => v.ForumPostId == post.Id && v.VoteType == VoteType.Up);
            post.DownvoteCount = votes.Count(v => v.ForumPostId == post.Id && v.VoteType == VoteType.Down);
            post.FlagCount = flags.Count(f => f.ForumPostId == post.Id);
            post.ReplyCount = replies.Count(r => r.ParentPostId == post.Id);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Seeded community: {C} categories, {P} posts ({R} replies, all moderation states), {V} votes, {F} flags",
            3, roots.Length, replies.Length, votes.Count, flags.Count);
    }
}
