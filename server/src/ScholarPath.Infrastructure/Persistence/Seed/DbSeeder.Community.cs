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
    /// votes / replies / flags.
    ///
    /// Categories are idempotent by slug — new ones can be appended on
    /// subsequent runs without disturbing existing data. Posts/votes/flags
    /// are idempotent on <see cref="ForumPost"/> being empty.
    /// </summary>
    private static async Task SeedCommunityAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // --- categories (idempotent by slug) ----------------------------
        // The full set of categories the platform ships with. Each row has
        // a stable slug used both as a deep-link key and as the dedup key
        // here, so adding a new category on a later seed run is a no-op
        // for the existing rows.
        var desiredCategories = new[]
        {
            new ForumCategory { NameEn = "General Discussion", NameAr = "نقاش عام", Slug = "general-discussion", DescriptionEn = "Open discussion about anything related to scholarships, study abroad and student life.", DescriptionAr = "نقاش مفتوح حول كل ما يتعلق بالمنح الدراسية والدراسة في الخارج وحياة الطلاب.", DisplayOrder = 1, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Scholarship Tips", NameAr = "نصائح المنح", Slug = "scholarship-tips", DescriptionEn = "Practical tips and tricks for finding and winning scholarships.", DescriptionAr = "نصائح وحيل عملية للعثور على المنح والفوز بها.", DisplayOrder = 2, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Application Process", NameAr = "عملية التقديم", Slug = "application-process", DescriptionEn = "Help with scholarship applications, essays and personal statements.", DescriptionAr = "المساعدة في تقديم طلبات المنح والمقالات والبيانات الشخصية.", DisplayOrder = 3, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Visa & Travel", NameAr = "التأشيرة والسفر", Slug = "visa-travel", DescriptionEn = "Visa applications, embassy interviews, flights and arrival logistics.", DescriptionAr = "طلبات التأشيرة ومقابلات السفارة والرحلات وترتيبات الوصول.", DisplayOrder = 4, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Country-Specific", NameAr = "حسب الدولة", Slug = "country-specific", DescriptionEn = "Tips, requirements and experiences for specific destination countries.", DescriptionAr = "نصائح ومتطلبات وتجارب لدول الدراسة المختلفة.", DisplayOrder = 5, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Field-Specific", NameAr = "حسب التخصص", Slug = "field-specific", DescriptionEn = "Discussions grouped by field of study — STEM, business, arts and more.", DescriptionAr = "نقاشات مجمعة حسب التخصص — العلوم والتكنولوجيا والأعمال والفنون وغيرها.", DisplayOrder = 6, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Success Stories", NameAr = "قصص نجاح", Slug = "success-stories", DescriptionEn = "Share your scholarship and study-abroad success — and read others' journeys.", DescriptionAr = "شارك قصة نجاحك مع المنح والدراسة في الخارج — واقرأ قصص الآخرين.", DisplayOrder = 7, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Q&A", NameAr = "أسئلة وإجابات", Slug = "questions-answers", DescriptionEn = "Ask anything — the community and consultants will help.", DescriptionAr = "اسأل عن أي شيء — المجتمع والمستشارون سيساعدون.", DisplayOrder = 8, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Funding & Finance", NameAr = "التمويل والمالية", Slug = "funding-finance", DescriptionEn = "Discuss funding options, stipends and budgeting abroad.", DescriptionAr = "مناقشة خيارات التمويل والرواتب والميزانية في الخارج.", DisplayOrder = 9, IsActive = true, CreatedAt = now.AddDays(-120) },
            new ForumCategory { NameEn = "Student Life Abroad", NameAr = "حياة الطالب في الخارج", Slug = "student-life-abroad", DescriptionEn = "Housing, friendships and settling into a new country.", DescriptionAr = "السكن والصداقات والاستقرار في بلد جديد.", DisplayOrder = 10, IsActive = true, CreatedAt = now.AddDays(-120) },
        };

        var existingSlugs = await db.ForumCategories
            .Select(c => c.Slug)
            .ToListAsync(ct).ConfigureAwait(false);

        var newCategories = desiredCategories.Where(c => !existingSlugs.Contains(c.Slug)).ToList();
        if (newCategories.Count > 0)
        {
            db.ForumCategories.AddRange(newCategories);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation("Seeded {N} forum categories", newCategories.Count);
        }

        // Post/vote/flag seeding is one-shot — guarded on posts being empty
        // so a partial re-run can't double-insert threaded content.
        if (await db.ForumPosts.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        // Look up the canonical category rows by slug so post wiring is stable
        // even if categories were created across multiple seed runs.
        var allCategories = await db.ForumCategories.ToListAsync(ct).ConfigureAwait(false);
        var byslug = allCategories.ToDictionary(c => c.Slug, c => c);

        var catGeneral = byslug["general-discussion"];
        var catTips = byslug["scholarship-tips"];
        var catApplication = byslug["application-process"];
        var catVisa = byslug["visa-travel"];
        var catCountry = byslug["country-specific"];
        var catField = byslug["field-specific"];
        var catSuccess = byslug["success-stories"];
        var catQa = byslug["questions-answers"];
        var catFunding = byslug["funding-finance"];
        var catLife = byslug["student-life-abroad"];

        // --- root posts (20+ across categories, mostly Visible with a few in
        //     PendingReview / Hidden / Removed to cover the moderation UI) ---
        var visibleApp = new ForumPost
        {
            AuthorId = users.Students[0].Id,
            CategoryId = catApplication.Id,
            Title = "How long should a statement of purpose be?",
            BodyMarkdown = "I'm applying for a Master's scholarship and the guidance only says \"concise\". Is 800 words too long?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-14),
        };
        var visibleFunding = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catFunding.Id,
            Title = "Fully funded vs partially funded — is the difference worth it?",
            BodyMarkdown = "I have an offer for a partially funded scholarship but I'm waiting on a fully funded one. Should I hold out?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-9),
        };
        var visibleTips = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catTips.Id,
            Title = "My five-step shortlist process for scholarships",
            BodyMarkdown = "I went from 60 saved scholarships to a focused list of 8 in two evenings. Sharing the filter I used: eligibility -> deadline -> funding type -> field fit -> effort to apply.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-11),
        };
        var visibleVisa = new ForumPost
        {
            AuthorId = users.Students[3].Id,
            CategoryId = catVisa.Id,
            Title = "UK Student visa — financial evidence questions",
            BodyMarkdown = "Has anyone here used a parent's bank statement as financial evidence for the UK student visa? The 28-day rule is confusing me.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-8),
        };
        var visibleCountry = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catCountry.Id,
            Title = "Studying in Germany — public vs private universities",
            BodyMarkdown = "Public universities in Germany are tuition-free for international students in most states. Has anyone weighed that against a private university with merit aid?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-12),
        };
        var visibleField = new ForumPost
        {
            AuthorId = users.Consultants[0].Id,
            CategoryId = catField.Id,
            Title = "Computer Science scholarships — which countries lead in 2026?",
            BodyMarkdown = "Posting a working list of CS-friendly funding programmes. Germany (DAAD), Netherlands (Holland Scholarship), Canada (Vanier for PhD), Korea (KGSP). Add yours below.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-15),
        };
        var visibleSuccess = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catSuccess.Id,
            Title = "I got the STEM Excellence Award — full breakdown of my application",
            BodyMarkdown = "Just received the offer this week. Sharing what I wish I had known: start the personal statement six weeks early, get three different people to read it, and follow the prompt exactly.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-20),
        };
        var visibleQa = new ForumPost
        {
            AuthorId = users.Students[0].Id,
            CategoryId = catQa.Id,
            Title = "Can I use the same recommendation letter for multiple scholarships?",
            BodyMarkdown = "My professor wrote a strong letter. Is it OK to send the same one to three scholarship committees, or should I ask for tailored versions?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-6),
        };
        var visibleGeneral = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catGeneral.Id,
            Title = "Anyone else applying for fall 2026 intake?",
            BodyMarkdown = "Curious how many of us are racing the same set of deadlines this autumn. Drop your target countries below.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-10),
        };
        var visibleLife = new ForumPost
        {
            AuthorId = users.Consultants[1].Id,
            CategoryId = catLife.Id,
            Title = "First-week-abroad checklist that actually matters",
            BodyMarkdown = "Forget the long Pinterest lists. The first week is really about: registering with the local authorities, opening a bank account, getting a SIM card, and finding your nearest grocery store.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-18),
        };
        var visibleTips2 = new ForumPost
        {
            AuthorId = users.Consultants[2].Id,
            CategoryId = catTips.Id,
            Title = "Three early-deadline scholarships most students miss",
            BodyMarkdown = "Erasmus Mundus (mid-October), Chevening (early November) and Australia Awards (late April). Mark these on your calendar — they close months before the bulk of applications.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-25),
        };
        var visibleApplication2 = new ForumPost
        {
            AuthorId = users.Students[3].Id,
            CategoryId = catApplication.Id,
            Title = "How to ask for a strong recommendation letter",
            BodyMarkdown = "I'm shy about asking my supervisor. What's the most professional way to request a letter, and what materials should I send with the request?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-7),
        };
        var visibleVisa2 = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catVisa.Id,
            Title = "Schengen student visa — how early should I apply?",
            BodyMarkdown = "My programme starts in late September. Some consulates accept applications three months in advance — is it worth applying as soon as the window opens?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-13),
        };
        var visibleCountry2 = new ForumPost
        {
            AuthorId = users.Consultants[0].Id,
            CategoryId = catCountry.Id,
            Title = "Canada vs Australia — choosing between two offers",
            BodyMarkdown = "Both offered partial funding. Cost of living, post-graduation work rights, and weather are the three things I'd weigh most heavily. What's been your experience?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-16),
        };
        var visibleField2 = new ForumPost
        {
            AuthorId = users.Students[0].Id,
            CategoryId = catField.Id,
            Title = "Looking for business school scholarships in Europe",
            BodyMarkdown = "MBA or MIM, ideally with at least a tuition waiver. Has anyone applied to HEC Paris, IE, or Bocconi scholarships?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-5),
        };
        var visibleSuccess2 = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catSuccess.Id,
            Title = "From rejection to acceptance in one year",
            BodyMarkdown = "I was rejected from my top choice last year. After working with a consultant on this platform and rebuilding my statement, I got in this cycle with a 60% tuition waiver.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-22),
        };
        var visibleQa2 = new ForumPost
        {
            AuthorId = users.Students[3].Id,
            CategoryId = catQa.Id,
            Title = "Is IELTS Academic accepted for all Chevening universities?",
            BodyMarkdown = "Most UK universities accept IELTS Academic, but I'm seeing a few that ask for additional language evidence. Anyone navigated this for Chevening?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-4),
        };
        var visibleGeneral2 = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catGeneral.Id,
            Title = "How do you stay motivated through a long application cycle?",
            BodyMarkdown = "Between essays, recommendations and waiting for responses, the process can take six months or more. How do you avoid burning out?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-17),
        };
        var visibleLife2 = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catLife.Id,
            Title = "Cultural shock — what helped you adjust?",
            BodyMarkdown = "Moved from Cairo to Berlin three months ago. Some days are easy, some are hard. What helped you find your footing in a new country?",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-21),
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
            CategoryId = catApplication.Id,
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

        var roots = new[]
        {
            visibleApp, visibleFunding, visibleTips, visibleVisa, visibleCountry,
            visibleField, visibleSuccess, visibleQa, visibleGeneral, visibleLife,
            visibleTips2, visibleApplication2, visibleVisa2, visibleCountry2, visibleField2,
            visibleSuccess2, visibleQa2, visibleGeneral2, visibleLife2,
            pendingPost, hiddenPost, removedPost,
        };
        db.ForumPosts.AddRange(roots);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- replies (non-null ParentPostId) -----------------------------
        var reply1 = new ForumPost
        {
            AuthorId = users.Consultants[0].Id,
            CategoryId = catApplication.Id,
            ParentPostId = visibleApp.Id,
            BodyMarkdown = "800 words is fine if every sentence earns its place. Most committees expect 700–900.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-13),
        };
        var reply2 = new ForumPost
        {
            AuthorId = users.Students[2].Id,
            CategoryId = catApplication.Id,
            ParentPostId = visibleApp.Id,
            BodyMarkdown = "Agreed — I trimmed mine from 1,100 to 850 and it read much better.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-12),
        };
        var reply3 = new ForumPost
        {
            AuthorId = users.Consultants[1].Id,
            CategoryId = catFunding.Id,
            ParentPostId = visibleFunding.Id,
            BodyMarkdown = "If the fully funded offer covers living costs, the difference is usually worth a short wait.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-8),
        };
        var reply4 = new ForumPost
        {
            AuthorId = users.Students[0].Id,
            CategoryId = catTips.Id,
            ParentPostId = visibleTips.Id,
            BodyMarkdown = "Bookmarking this. The \"effort to apply\" filter is something I never thought to score.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-10),
        };
        var reply5 = new ForumPost
        {
            AuthorId = users.Consultants[2].Id,
            CategoryId = catVisa.Id,
            ParentPostId = visibleVisa.Id,
            BodyMarkdown = "Yes, parent statements are fine — just make sure the 28-day rule is met when you submit, and have a signed sponsorship letter.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-7),
        };
        var reply6 = new ForumPost
        {
            AuthorId = users.Students[3].Id,
            CategoryId = catCountry.Id,
            ParentPostId = visibleCountry.Id,
            BodyMarkdown = "Public unis are great academically but housing waiting lists can be long. Apply for accommodation the same day you accept the admission offer.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-11),
        };
        var reply7 = new ForumPost
        {
            AuthorId = users.Students[1].Id,
            CategoryId = catSuccess.Id,
            ParentPostId = visibleSuccess.Id,
            BodyMarkdown = "Congratulations! The \"start six weeks early\" advice is the one I always under-rate.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-19),
        };
        var reply8 = new ForumPost
        {
            AuthorId = users.Consultants[0].Id,
            CategoryId = catQa.Id,
            ParentPostId = visibleQa.Id,
            BodyMarkdown = "Tailor at least a paragraph per committee. A purely generic letter often reads that way to reviewers.",
            ModerationStatus = PostModerationStatus.Visible,
            CreatedAt = now.AddDays(-5),
        };

        var replies = new[] { reply1, reply2, reply3, reply4, reply5, reply6, reply7, reply8 };
        db.ForumPosts.AddRange(replies);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- votes (unique per (post, user)) -----------------------------
        var votes = new List<ForumVote>
        {
            new() { ForumPostId = visibleApp.Id, UserId = users.Students[1].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-13) },
            new() { ForumPostId = visibleApp.Id, UserId = users.Students[2].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = visibleApp.Id, UserId = users.Consultants[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = visibleFunding.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-8) },
            new() { ForumPostId = visibleFunding.Id, UserId = users.Students[3].Id, VoteType = VoteType.Down, VotedAt = now.AddDays(-7) },
            new() { ForumPostId = visibleTips.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-10) },
            new() { ForumPostId = visibleTips.Id, UserId = users.Students[3].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-9) },
            new() { ForumPostId = visibleSuccess.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-19) },
            new() { ForumPostId = visibleSuccess.Id, UserId = users.Students[1].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-18) },
            new() { ForumPostId = visibleSuccess.Id, UserId = users.Consultants[1].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-18) },
            new() { ForumPostId = visibleVisa.Id, UserId = users.Students[2].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-7) },
            new() { ForumPostId = reply1.Id, UserId = users.Students[0].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-12) },
            new() { ForumPostId = reply5.Id, UserId = users.Students[3].Id, VoteType = VoteType.Up, VotedAt = now.AddDays(-7) },
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
            desiredCategories.Length, roots.Length, replies.Length, votes.Count, flags.Count);
    }
}
