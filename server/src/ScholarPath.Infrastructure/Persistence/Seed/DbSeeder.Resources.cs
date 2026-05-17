using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the learning-resources library: <see cref="Resource"/> rows
    /// covering EVERY <see cref="ResourceStatus"/> (Draft / PendingReview /
    /// Published / Hidden / Removed) and EVERY <see cref="ResourceType"/>
    /// (Article / Guide / Checklist / VideoLink), authored by admins,
    /// companies and consultants, with multi-chapter <see cref="ResourceChild"/>
    /// content for the guides. A couple are featured. Idempotent on
    /// <see cref="Resource"/> being empty. Returns the seeded resources so the
    /// engagement seeder can attach bookmarks / progress.
    /// </summary>
    private static async Task<IReadOnlyList<Resource>> SeedResourcesAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        var existing = await db.Resources.IgnoreQueryFilters()
            .Include(r => r.Chapters)
            .ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var admin = users.PrimaryAdmin.Id;

        var resources = new List<Resource>
        {
            // Published Guide — featured #1, multi-chapter.
            new()
            {
                TitleEn = "The Complete Scholarship Application Guide",
                TitleAr = "الدليل الكامل للتقديم على المنح الدراسية",
                Slug = "complete-scholarship-application-guide",
                DescriptionEn = "An end-to-end walkthrough of the scholarship application process, from research to submission.",
                DescriptionAr = "شرح متكامل لعملية التقديم على المنح، من البحث حتى تقديم الطلب.",
                ContentMarkdownEn = "# Overview\nThis guide is split into chapters covering every stage of an application.",
                ContentMarkdownAr = "# نظرة عامة\nينقسم هذا الدليل إلى فصول تغطي كل مرحلة من مراحل التقديم.",
                AuthorUserId = admin,
                AuthorRole = "Admin",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "applications",
                TagsJson = """["guide","applications","beginner"]""",
                IsFeatured = true,
                FeaturedOrder = 1,
                PublishedAt = now.AddDays(-40),
                ReviewedAt = now.AddDays(-41),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-45),
            },
            // Published Article — featured #2.
            new()
            {
                TitleEn = "Five Mistakes That Sink Scholarship Essays",
                TitleAr = "خمسة أخطاء تُفشل مقالات المنح الدراسية",
                Slug = "five-mistakes-scholarship-essays",
                DescriptionEn = "The most common writing mistakes reviewers see — and how to avoid them.",
                DescriptionAr = "أكثر الأخطاء الكتابية شيوعاً التي يراها المراجعون وكيفية تجنبها.",
                ContentMarkdownEn = "# Five Mistakes\n1. Generic openings\n2. Listing instead of storytelling\n3. Ignoring the prompt\n4. Weak conclusions\n5. Skipping proofreading",
                ContentMarkdownAr = "# خمسة أخطاء\n1. مقدمات عامة\n2. السرد على شكل قوائم\n3. تجاهل السؤال\n4. خواتيم ضعيفة\n5. إهمال المراجعة اللغوية",
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "essays",
                TagsJson = """["article","essays","writing"]""",
                IsFeatured = true,
                FeaturedOrder = 2,
                PublishedAt = now.AddDays(-25),
                ReviewedAt = now.AddDays(-26),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-30),
            },
            // Published Checklist.
            new()
            {
                TitleEn = "Pre-Submission Document Checklist",
                TitleAr = "قائمة التحقق من المستندات قبل التقديم",
                Slug = "pre-submission-document-checklist",
                DescriptionEn = "Everything to gather and verify before you hit submit.",
                DescriptionAr = "كل ما يجب تجهيزه والتحقق منه قبل الضغط على زر التقديم.",
                ContentMarkdownEn = "- [ ] Transcript\n- [ ] Recommendation letters\n- [ ] Personal statement\n- [ ] Proof of English\n- [ ] Passport copy",
                ContentMarkdownAr = "- [ ] كشف الدرجات\n- [ ] خطابات التوصية\n- [ ] البيان الشخصي\n- [ ] إثبات اللغة الإنجليزية\n- [ ] نسخة جواز السفر",
                AuthorUserId = users.Companies[0].Id,
                AuthorRole = "Company",
                Type = ResourceType.Checklist,
                Status = ResourceStatus.Published,
                CategorySlug = "applications",
                TagsJson = """["checklist","documents"]""",
                PublishedAt = now.AddDays(-15),
                ReviewedAt = now.AddDays(-16),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-20),
            },
            // Published VideoLink.
            new()
            {
                TitleEn = "Webinar: Interview Preparation Masterclass",
                TitleAr = "ندوة: الدرس المتقدم في التحضير للمقابلة",
                Slug = "webinar-interview-prep-masterclass",
                DescriptionEn = "A recorded masterclass on acing scholarship and admissions interviews.",
                DescriptionAr = "درس متقدم مسجل حول التفوق في مقابلات المنح والقبول.",
                ExternalLinkUrl = "https://videos.scholarpath.local/interview-masterclass",
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "interviews",
                TagsJson = """["video","interview"]""",
                PublishedAt = now.AddDays(-10),
                ReviewedAt = now.AddDays(-11),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-12),
            },
            // PendingReview — submitted, awaiting admin approval.
            new()
            {
                TitleEn = "Budgeting for Life as an International Student",
                TitleAr = "إعداد الميزانية للحياة كطالب دولي",
                Slug = "budgeting-international-student",
                DescriptionEn = "A practical budgeting guide submitted for admin review.",
                DescriptionAr = "دليل عملي لإعداد الميزانية مقدم لمراجعة الإدارة.",
                ContentMarkdownEn = "# Budgeting\nTrack rent, food, transport and insurance from day one.",
                ContentMarkdownAr = "# الميزانية\nتابع الإيجار والطعام والمواصلات والتأمين من اليوم الأول.",
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Guide,
                Status = ResourceStatus.PendingReview,
                CategorySlug = "finance",
                TagsJson = """["guide","finance"]""",
                CreatedAt = now.AddDays(-3),
            },
            // Draft — author still writing.
            new()
            {
                TitleEn = "Choosing the Right Country to Study In (Draft)",
                TitleAr = "اختيار البلد المناسب للدراسة (مسودة)",
                Slug = "choosing-the-right-country-draft",
                DescriptionEn = "A work-in-progress comparison of popular study destinations.",
                DescriptionAr = "مقارنة قيد الإعداد لأشهر وجهات الدراسة.",
                ContentMarkdownEn = "# Draft\nDestination comparison coming soon.",
                ContentMarkdownAr = "# مسودة\nمقارنة الوجهات قريباً.",
                AuthorUserId = users.Companies[1].Id,
                AuthorRole = "Company",
                Type = ResourceType.Article,
                Status = ResourceStatus.Draft,
                CategorySlug = "planning",
                TagsJson = """["draft","planning"]""",
                CreatedAt = now.AddDays(-2),
            },
            // Hidden — published then hidden by an admin.
            new()
            {
                TitleEn = "Outdated Visa Process Overview",
                TitleAr = "نظرة عامة قديمة على إجراءات التأشيرة",
                Slug = "outdated-visa-process-overview",
                DescriptionEn = "An older visa overview hidden because the rules have changed.",
                DescriptionAr = "نظرة عامة قديمة على التأشيرة تم إخفاؤها لتغير القواعد.",
                ContentMarkdownEn = "# Visa Process\nThis content is out of date.",
                ContentMarkdownAr = "# إجراءات التأشيرة\nهذا المحتوى قديم.",
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Hidden,
                CategorySlug = "visas",
                TagsJson = """["article","visa"]""",
                PublishedAt = now.AddDays(-120),
                ReviewedAt = now.AddDays(-121),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-125),
            },
            // Removed — rejected by an admin with a reason.
            new()
            {
                TitleEn = "Removed: Promotional Content",
                TitleAr = "محتوى ترويجي تمت إزالته",
                Slug = "removed-promotional-content",
                DescriptionEn = "A submission removed for breaching the content policy.",
                DescriptionAr = "محتوى مقدَّم تمت إزالته لمخالفته سياسة المحتوى.",
                ContentMarkdownEn = "# Removed\nThis resource violated the content policy.",
                ContentMarkdownAr = "# تمت الإزالة\nخالف هذا المحتوى سياسة المحتوى.",
                AuthorUserId = users.Companies[2].Id,
                AuthorRole = "Company",
                Type = ResourceType.Article,
                Status = ResourceStatus.Removed,
                CategorySlug = "misc",
                TagsJson = """["removed"]""",
                ReviewedAt = now.AddDays(-5),
                ReviewedByAdminId = admin,
                RejectionReason = "Promotional / advertising content is not permitted in the resource library.",
                CreatedAt = now.AddDays(-8),
            },
        };

        db.Resources.AddRange(resources);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Chapters for the two Guides (the first guide + the pending-review guide).
        var chapters = new List<ResourceChild>();
        var mainGuide = resources[0];
        chapters.AddRange(
        [
            new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Researching scholarships", TitleAr = "البحث عن المنح", ContentMarkdownEn = "Use category filters and deadlines to build a shortlist.", ContentMarkdownAr = "استخدم فلاتر التصنيف والمواعيد لبناء قائمة مختصرة.", SortOrder = 1, EstimatedReadMinutes = 6 },
            new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Preparing your documents", TitleAr = "تجهيز مستنداتك", ContentMarkdownEn = "Gather transcripts, references and a tailored statement.", ContentMarkdownAr = "اجمع كشوف الدرجات والتوصيات وبياناً مخصصاً.", SortOrder = 2, EstimatedReadMinutes = 8 },
            new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Writing the personal statement", TitleAr = "كتابة البيان الشخصي", ContentMarkdownEn = "Tell a focused story that answers the prompt.", ContentMarkdownAr = "اروِ قصة مركزة تجيب عن السؤال.", SortOrder = 3, EstimatedReadMinutes = 10 },
            new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Submitting and following up", TitleAr = "التقديم والمتابعة", ContentMarkdownEn = "Submit early and track the application status.", ContentMarkdownAr = "قدّم مبكراً وتابع حالة الطلب.", SortOrder = 4, EstimatedReadMinutes = 5 },
        ]);

        var pendingGuide = resources.First(r => r.Status == ResourceStatus.PendingReview);
        chapters.AddRange(
        [
            new ResourceChild { ResourceId = pendingGuide.Id, TitleEn = "Fixed vs variable costs", TitleAr = "التكاليف الثابتة والمتغيرة", ContentMarkdownEn = "Separate rent and insurance from food and leisure.", ContentMarkdownAr = "افصل الإيجار والتأمين عن الطعام والترفيه.", SortOrder = 1, EstimatedReadMinutes = 5 },
            new ResourceChild { ResourceId = pendingGuide.Id, TitleEn = "Building an emergency fund", TitleAr = "بناء صندوق للطوارئ", ContentMarkdownEn = "Aim for one month of expenses in reserve.", ContentMarkdownAr = "استهدف ادخار نفقات شهر كاحتياطي.", SortOrder = 2, EstimatedReadMinutes = 4 },
        ]);

        db.ResourceChapters.AddRange(chapters);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} resources (+{C} chapters) covering all statuses and types", resources.Count, chapters.Count);

        // Re-load with chapters populated so the engagement seeder can use them.
        return await db.Resources.IgnoreQueryFilters()
            .Include(r => r.Chapters)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds resource engagement: <see cref="ResourceBookmark"/>s and
    /// <see cref="ResourceProgress"/> records (with per-chapter
    /// <see cref="ResourceProgressChild"/> rows) for the demo students against
    /// the published, chaptered resources. Idempotent on
    /// <see cref="ResourceBookmark"/> being empty.
    /// </summary>
    private static async Task SeedResourceEngagementAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<Resource> resources,
        ILogger logger, CancellationToken ct)
    {
        if (await db.ResourceBookmarks.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var published = resources.Where(r => r.Status == ResourceStatus.Published).ToList();
        if (published.Count == 0)
        {
            return;
        }

        // --- bookmarks (unique per (user, resource)) ---------------------
        var bookmarks = new List<ResourceBookmark>
        {
            new() { UserId = users.Students[0].Id, ResourceId = published[0].Id, BookmarkedAt = now.AddDays(-12) },
            new() { UserId = users.Students[0].Id, ResourceId = published[Math.Min(1, published.Count - 1)].Id, BookmarkedAt = now.AddDays(-9) },
            new() { UserId = users.Students[1].Id, ResourceId = published[0].Id, BookmarkedAt = now.AddDays(-8) },
            new() { UserId = users.Students[2].Id, ResourceId = published[Math.Min(2, published.Count - 1)].Id, BookmarkedAt = now.AddDays(-5) },
        };
        db.ResourceBookmarks.AddRange(bookmarks.DistinctBy(b => (b.UserId, b.ResourceId)));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // --- progress on the multi-chapter main guide --------------------
        var guide = published.FirstOrDefault(r => r.Chapters.Count > 0);
        var progressCount = 0;
        if (guide is not null)
        {
            var orderedChapters = guide.Chapters.OrderBy(c => c.SortOrder).ToList();

            // Student 0 — partly through (first 2 of N chapters complete).
            var partial = new ResourceProgress
            {
                UserId = users.Students[0].Id,
                ResourceId = guide.Id,
                ChaptersCompletedCount = Math.Min(2, orderedChapters.Count),
                LastAccessedAt = now.AddDays(-4),
                CreatedAt = now.AddDays(-11),
            };
            // Student 1 — finished every chapter.
            var complete = new ResourceProgress
            {
                UserId = users.Students[1].Id,
                ResourceId = guide.Id,
                ChaptersCompletedCount = orderedChapters.Count,
                LastAccessedAt = now.AddDays(-2),
                CreatedAt = now.AddDays(-8),
            };
            db.ResourceProgress.AddRange(partial, complete);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            progressCount = 2;

            var childRows = new List<ResourceProgressChild>();
            for (var i = 0; i < orderedChapters.Count; i++)
            {
                var done = i < partial.ChaptersCompletedCount;
                childRows.Add(new ResourceProgressChild
                {
                    ResourceProgressId = partial.Id,
                    ResourceChildId = orderedChapters[i].Id,
                    IsCompleted = done,
                    CompletedAt = done ? now.AddDays(-5 - i) : null,
                });
                childRows.Add(new ResourceProgressChild
                {
                    ResourceProgressId = complete.Id,
                    ResourceChildId = orderedChapters[i].Id,
                    IsCompleted = true,
                    CompletedAt = now.AddDays(-7 + i),
                });
            }

            db.ResourceProgressChildren.AddRange(childRows);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Seeded resource engagement: {B} bookmarks, {P} progress records",
            bookmarks.Count, progressCount);
    }
}
