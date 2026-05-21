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
            // Resources already seeded. Back-fill chapters when an earlier run
            // created the resources before chapter seeding existed.
            await SeedResourceChaptersAsync(db, existing, logger, ct).ConfigureAwait(false);

            // Back-fill external video URLs that used the unresolvable
            // `videos.scholarpath.local` placeholder host. The placeholder
            // shipped in the original seed and 404s in browsers — swap each
            // entry for a YouTube search that always resolves.
            await BackfillVideoUrlsAsync(db, existing, logger, ct).ConfigureAwait(false);
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        var admin = users.PrimaryAdmin.Id;

        // Authors rotate across admin / consultants / companies so the
        // "by Admin" / "by Consultant" / "by Company" filters in the hub
        // all have results. The catalogue covers the full editorial surface:
        // 9 articles, 6 guides, 6 videos and 5 checklists at Published
        // status, plus one Draft, one Hidden and one Removed for moderation
        // state coverage.
        var resources = new List<Resource>
        {
            // ─── Featured guide #1 — flagship multi-chapter walkthrough ──
            new()
            {
                TitleEn = "The Complete Scholarship Application Guide",
                TitleAr = "الدليل الكامل للتقديم على المنح الدراسية",
                Slug = "complete-scholarship-application-guide",
                DescriptionEn = "An end-to-end walkthrough of the scholarship application process, from research to submission.",
                DescriptionAr = "شرح متكامل لعملية التقديم على المنح، من البحث حتى تقديم الطلب.",
                ContentMarkdownEn = ResourceBodies.CompleteGuideEn,
                ContentMarkdownAr = ResourceBodies.CompleteGuideAr,
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
            // ─── Featured article #2 ─────────────────────────────────────
            new()
            {
                TitleEn = "Five Mistakes That Sink Scholarship Essays",
                TitleAr = "خمسة أخطاء تُفشل مقالات المنح الدراسية",
                Slug = "five-mistakes-scholarship-essays",
                DescriptionEn = "The most common writing mistakes reviewers see — and how to avoid them.",
                DescriptionAr = "أكثر الأخطاء الكتابية شيوعاً التي يراها المراجعون وكيفية تجنبها.",
                ContentMarkdownEn = ResourceBodies.FiveMistakesEn,
                ContentMarkdownAr = ResourceBodies.FiveMistakesAr,
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
            // ─── Articles (8+) ───────────────────────────────────────────
            new()
            {
                TitleEn = "Statement of Purpose: A Practical Writing Guide",
                TitleAr = "خطاب الغرض: دليل كتابة عملي",
                Slug = "statement-of-purpose-practical-guide",
                DescriptionEn = "How to plan, draft and polish a statement of purpose that actually answers the prompt.",
                DescriptionAr = "كيفية التخطيط والكتابة وصقل خطاب الغرض بحيث يجيب فعلاً عن السؤال المطروح.",
                ContentMarkdownEn = ResourceBodies.SopEn,
                ContentMarkdownAr = ResourceBodies.SopAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "essays",
                TagsJson = """["article","sop","essays"]""",
                IsFeatured = true,
                FeaturedOrder = 3,
                PublishedAt = now.AddDays(-35),
                ReviewedAt = now.AddDays(-36),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-38),
            },
            new()
            {
                TitleEn = "Recommendation Letters: Asking and Following Up",
                TitleAr = "خطابات التوصية: كيف تطلبها وتتابعها",
                Slug = "recommendation-letters-asking-following-up",
                DescriptionEn = "Who to ask, what to send them, and how to follow up without nagging.",
                DescriptionAr = "ممن تطلب التوصية، وما الذي ترسله معها، وكيف تتابع دون إلحاح.",
                ContentMarkdownEn = ResourceBodies.RecommendationEn,
                ContentMarkdownAr = ResourceBodies.RecommendationAr,
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "applications",
                TagsJson = """["article","recommendations"]""",
                PublishedAt = now.AddDays(-28),
                ReviewedAt = now.AddDays(-29),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-32),
            },
            new()
            {
                TitleEn = "Interview Preparation: What Selection Panels Look For",
                TitleAr = "التحضير للمقابلة: ما تبحث عنه لجان الاختيار",
                Slug = "interview-preparation-selection-panels",
                DescriptionEn = "Decode the most common interview questions and practise answers that stay on-message.",
                DescriptionAr = "افهم أكثر أسئلة المقابلة شيوعاً ودرّب نفسك على إجابات تبقى ضمن رسالتك.",
                ContentMarkdownEn = ResourceBodies.InterviewEn,
                ContentMarkdownAr = ResourceBodies.InterviewAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "interviews",
                TagsJson = """["article","interview"]""",
                PublishedAt = now.AddDays(-18),
                ReviewedAt = now.AddDays(-19),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-22),
            },
            new()
            {
                TitleEn = "IELTS Strategy: Score-Boosting Tactics for Each Section",
                TitleAr = "استراتيجية الآيلتس: تكتيكات لرفع الدرجة في كل قسم",
                Slug = "ielts-strategy-each-section",
                DescriptionEn = "Section-by-section study plan with target scores and the highest-yield tactics for each.",
                DescriptionAr = "خطة دراسة قسم-قسم مع الدرجات المستهدفة والتكتيكات الأكثر فاعلية لكل قسم.",
                ContentMarkdownEn = ResourceBodies.IeltsEn,
                ContentMarkdownAr = ResourceBodies.IeltsAr,
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "language",
                TagsJson = """["article","ielts","language"]""",
                PublishedAt = now.AddDays(-33),
                ReviewedAt = now.AddDays(-34),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-36),
            },
            new()
            {
                TitleEn = "TOEFL Preparation in 30 Days",
                TitleAr = "التحضير لاختبار التوفل في 30 يوماً",
                Slug = "toefl-preparation-30-days",
                DescriptionEn = "A 30-day TOEFL study plan with practice resources and pace milestones.",
                DescriptionAr = "خطة دراسية للتوفل لمدة 30 يوماً مع موارد تدريبية ومحطات قياس التقدم.",
                ContentMarkdownEn = ResourceBodies.ToeflEn,
                ContentMarkdownAr = ResourceBodies.ToeflAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "language",
                TagsJson = """["article","toefl","language"]""",
                PublishedAt = now.AddDays(-26),
                ReviewedAt = now.AddDays(-27),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-29),
            },
            new()
            {
                TitleEn = "Financial Aid Options Beyond the Headline Scholarship",
                TitleAr = "خيارات المساعدة المالية بخلاف المنح الرئيسية",
                Slug = "financial-aid-options-beyond-headline",
                DescriptionEn = "Tuition waivers, departmental grants, work-study programmes and emergency funds — the funding mix nobody tells you about.",
                DescriptionAr = "إعفاءات الرسوم والمنح الداخلية وبرامج العمل-الدراسة وصناديق الطوارئ — المزيج التمويلي الذي لا يخبرك به أحد.",
                ContentMarkdownEn = ResourceBodies.FinAidEn,
                ContentMarkdownAr = ResourceBodies.FinAidAr,
                AuthorUserId = users.Companies[1].Id,
                AuthorRole = "Company",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "finance",
                TagsJson = """["article","finance","funding"]""",
                PublishedAt = now.AddDays(-21),
                ReviewedAt = now.AddDays(-22),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-24),
            },
            new()
            {
                TitleEn = "Cultural Adaptation Abroad — The First 90 Days",
                TitleAr = "التكيف الثقافي في الخارج — أول 90 يوماً",
                Slug = "cultural-adaptation-first-90-days",
                DescriptionEn = "Practical advice for the culture-shock window most international students hit between weeks four and twelve.",
                DescriptionAr = "نصائح عملية لفترة الصدمة الثقافية التي يمر بها معظم الطلاب الدوليين بين الأسبوع الرابع والثاني عشر.",
                ContentMarkdownEn = ResourceBodies.CulturalEn,
                ContentMarkdownAr = ResourceBodies.CulturalAr,
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "life-abroad",
                TagsJson = """["article","life-abroad","wellbeing"]""",
                PublishedAt = now.AddDays(-17),
                ReviewedAt = now.AddDays(-18),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-20),
            },
            new()
            {
                TitleEn = "Student Visa Application Tips — Avoiding Common Rejections",
                TitleAr = "نصائح طلب تأشيرة الدراسة — تجنّب أسباب الرفض الشائعة",
                Slug = "student-visa-application-tips",
                DescriptionEn = "What consular officers actually check, and the documentation gaps that lead to surprise rejections.",
                DescriptionAr = "ما الذي يفحصه ضباط القنصليات فعلاً، وأي ثغرات الوثائق التي تسبب رفضاً مفاجئاً.",
                ContentMarkdownEn = ResourceBodies.VisaTipsEn,
                ContentMarkdownAr = ResourceBodies.VisaTipsAr,
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Article,
                Status = ResourceStatus.Published,
                CategorySlug = "visas",
                TagsJson = """["article","visa"]""",
                PublishedAt = now.AddDays(-14),
                ReviewedAt = now.AddDays(-15),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-17),
            },
            // ─── Guides (6+) ─────────────────────────────────────────────
            // The flagship guide is #1 above. Five more follow.
            new()
            {
                TitleEn = "Building a Scholarship Shortlist That Actually Fits You",
                TitleAr = "بناء قائمة منح مختصرة تناسبك فعلاً",
                Slug = "building-scholarship-shortlist",
                DescriptionEn = "A structured method for filtering hundreds of scholarships down to a focused shortlist of strong fits.",
                DescriptionAr = "طريقة منظمة لتصفية مئات المنح إلى قائمة مختصرة من الفرص الأكثر ملاءمة.",
                ContentMarkdownEn = ResourceBodies.ShortlistGuideEn,
                ContentMarkdownAr = ResourceBodies.ShortlistGuideAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "planning",
                TagsJson = """["guide","planning","shortlist"]""",
                PublishedAt = now.AddDays(-37),
                ReviewedAt = now.AddDays(-38),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-40),
            },
            new()
            {
                TitleEn = "Document Preparation Playbook — Transcripts to Translations",
                TitleAr = "دليل تجهيز المستندات — من كشف الدرجات إلى الترجمات",
                Slug = "document-preparation-playbook",
                DescriptionEn = "Everything you need to gather, certify and translate — in the order you need it.",
                DescriptionAr = "كل ما تحتاج تجميعه وتوثيقه وترجمته — بالترتيب الذي ستحتاجه.",
                ContentMarkdownEn = ResourceBodies.DocumentGuideEn,
                ContentMarkdownAr = ResourceBodies.DocumentGuideAr,
                AuthorUserId = users.Companies[0].Id,
                AuthorRole = "Company",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "applications",
                TagsJson = """["guide","documents"]""",
                PublishedAt = now.AddDays(-31),
                ReviewedAt = now.AddDays(-32),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-34),
            },
            new()
            {
                TitleEn = "Funding Your First Year — Budget, Sources and Backups",
                TitleAr = "تمويل سنتك الأولى — الميزانية والمصادر والاحتياطيات",
                Slug = "funding-your-first-year",
                DescriptionEn = "A guide to stacking a scholarship with grants, part-time work and personal savings to cover a full year abroad.",
                DescriptionAr = "دليل لجمع المنحة مع المنح الأخرى والعمل الجزئي والمدخرات الشخصية لتغطية سنة كاملة في الخارج.",
                ContentMarkdownEn = ResourceBodies.FundingGuideEn,
                ContentMarkdownAr = ResourceBodies.FundingGuideAr,
                AuthorUserId = users.Companies[1].Id,
                AuthorRole = "Company",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "finance",
                TagsJson = """["guide","finance"]""",
                PublishedAt = now.AddDays(-23),
                ReviewedAt = now.AddDays(-24),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-26),
            },
            new()
            {
                TitleEn = "Visa Application Step-By-Step — From Acceptance to Boarding",
                TitleAr = "خطوات طلب التأشيرة — من القبول إلى الصعود للطائرة",
                Slug = "visa-application-step-by-step",
                DescriptionEn = "A timeline-driven playbook from the day you accept an offer to the day you board your flight.",
                DescriptionAr = "دليل زمني من يوم قبول العرض إلى يوم الصعود إلى الطائرة.",
                ContentMarkdownEn = ResourceBodies.VisaGuideEn,
                ContentMarkdownAr = ResourceBodies.VisaGuideAr,
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "visas",
                TagsJson = """["guide","visa"]""",
                PublishedAt = now.AddDays(-19),
                ReviewedAt = now.AddDays(-20),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-22),
            },
            new()
            {
                TitleEn = "Pre-Departure Guide — The Last Two Weeks Before You Fly",
                TitleAr = "دليل ما قبل السفر — الأسبوعان الأخيران قبل الطيران",
                Slug = "pre-departure-last-two-weeks",
                DescriptionEn = "Banking, insurance, packing and paperwork — what to handle in the final fortnight before departure.",
                DescriptionAr = "البنوك والتأمين والتعبئة والأوراق — ما يجب التعامل معه في الأسبوعين الأخيرين قبل السفر.",
                ContentMarkdownEn = ResourceBodies.PreDepartureEn,
                ContentMarkdownAr = ResourceBodies.PreDepartureAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Guide,
                Status = ResourceStatus.Published,
                CategorySlug = "life-abroad",
                TagsJson = """["guide","pre-departure"]""",
                PublishedAt = now.AddDays(-12),
                ReviewedAt = now.AddDays(-13),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-15),
            },
            // PendingReview Guide — submitted, awaiting admin approval.
            new()
            {
                TitleEn = "Budgeting for Life as an International Student",
                TitleAr = "إعداد الميزانية للحياة كطالب دولي",
                Slug = "budgeting-international-student",
                DescriptionEn = "A practical budgeting guide submitted for admin review.",
                DescriptionAr = "دليل عملي لإعداد الميزانية مقدم لمراجعة الإدارة.",
                ContentMarkdownEn = ResourceBodies.BudgetingGuideEn,
                ContentMarkdownAr = ResourceBodies.BudgetingGuideAr,
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Guide,
                Status = ResourceStatus.PendingReview,
                CategorySlug = "finance",
                TagsJson = """["guide","finance"]""",
                CreatedAt = now.AddDays(-3),
            },
            // ─── Videos (6+) ─────────────────────────────────────────────
            new()
            {
                TitleEn = "Webinar: Interview Preparation Masterclass",
                TitleAr = "ندوة: الدرس المتقدم في التحضير للمقابلة",
                Slug = "webinar-interview-prep-masterclass",
                DescriptionEn = "A recorded masterclass on acing scholarship and admissions interviews.",
                DescriptionAr = "درس متقدم مسجل حول التفوق في مقابلات المنح والقبول.",
                ContentMarkdownEn = ResourceBodies.WebinarInterviewEn,
                ContentMarkdownAr = ResourceBodies.WebinarInterviewAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=scholarship+interview+preparation+masterclass",
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
            new()
            {
                TitleEn = "Webinar: Crafting a Standout Statement of Purpose",
                TitleAr = "ندوة: كتابة خطاب غرض متميز",
                Slug = "webinar-standout-statement-of-purpose",
                DescriptionEn = "A 45-minute live session walking through three real personal statements, with rewrites.",
                DescriptionAr = "جلسة مباشرة لمدة 45 دقيقة تستعرض ثلاث بيانات شخصية حقيقية مع إعادة الكتابة.",
                ContentMarkdownEn = ResourceBodies.WebinarSopEn,
                ContentMarkdownAr = ResourceBodies.WebinarSopAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=how+to+write+statement+of+purpose+masters",
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "essays",
                TagsJson = """["video","essays","sop"]""",
                PublishedAt = now.AddDays(-24),
                ReviewedAt = now.AddDays(-25),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-27),
            },
            new()
            {
                TitleEn = "Workshop: IELTS Speaking Practice Live",
                TitleAr = "ورشة عمل: تدريب مباشر على المحادثة في الآيلتس",
                Slug = "workshop-ielts-speaking-practice",
                DescriptionEn = "Live mock interviews with examiner-style feedback. Recorded and indexed for self-review.",
                DescriptionAr = "مقابلات تجريبية مباشرة مع ملاحظات بنمط الممتحن. مسجلة ومفهرسة للمراجعة الذاتية.",
                ContentMarkdownEn = ResourceBodies.WorkshopIeltsEn,
                ContentMarkdownAr = ResourceBodies.WorkshopIeltsAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=IELTS+speaking+practice+with+feedback",
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "language",
                TagsJson = """["video","ielts","language"]""",
                PublishedAt = now.AddDays(-29),
                ReviewedAt = now.AddDays(-30),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-32),
            },
            new()
            {
                TitleEn = "Panel: Life in Germany as an International Student",
                TitleAr = "حلقة نقاش: الحياة في ألمانيا كطالب دولي",
                Slug = "panel-life-in-germany",
                DescriptionEn = "Three students share housing, finances and cultural notes from their first year in Germany.",
                DescriptionAr = "ثلاثة طلاب يتشاركون ملاحظاتهم عن السكن والتمويل والثقافة في سنتهم الأولى بألمانيا.",
                ContentMarkdownEn = ResourceBodies.PanelGermanyEn,
                ContentMarkdownAr = ResourceBodies.PanelGermanyAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=studying+in+germany+as+international+student",
                AuthorUserId = users.Companies[0].Id,
                AuthorRole = "Company",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "life-abroad",
                TagsJson = """["video","life-abroad","germany"]""",
                PublishedAt = now.AddDays(-16),
                ReviewedAt = now.AddDays(-17),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-19),
            },
            new()
            {
                TitleEn = "Webinar: Funding a Master's Degree With Multiple Sources",
                TitleAr = "ندوة: تمويل الماجستير من مصادر متعددة",
                Slug = "webinar-funding-masters-multiple-sources",
                DescriptionEn = "How to combine partial scholarships, departmental aid and work-study to cover a Master's degree.",
                DescriptionAr = "كيفية الجمع بين المنح الجزئية والمساعدات الداخلية والعمل-الدراسة لتمويل الماجستير.",
                ContentMarkdownEn = ResourceBodies.WebinarFundingEn,
                ContentMarkdownAr = ResourceBodies.WebinarFundingAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=how+to+fund+masters+degree+scholarships",
                AuthorUserId = users.Companies[1].Id,
                AuthorRole = "Company",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "finance",
                TagsJson = """["video","finance"]""",
                PublishedAt = now.AddDays(-13),
                ReviewedAt = now.AddDays(-14),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-16),
            },
            new()
            {
                TitleEn = "Walkthrough: Writing the Personal Statement Live",
                TitleAr = "شرح تفاعلي: كتابة البيان الشخصي مباشرة",
                Slug = "walkthrough-personal-statement-live",
                DescriptionEn = "A recorded screen-share of a real personal statement being drafted from outline to final paragraph.",
                DescriptionAr = "تسجيل مشاركة شاشة لكتابة بيان شخصي حقيقي من المخطط إلى الفقرة النهائية.",
                ContentMarkdownEn = ResourceBodies.WalkthroughPsEn,
                ContentMarkdownAr = ResourceBodies.WalkthroughPsAr,
                ExternalLinkUrl = "https://www.youtube.com/results?search_query=personal+statement+writing+walkthrough",
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.VideoLink,
                Status = ResourceStatus.Published,
                CategorySlug = "essays",
                TagsJson = """["video","essays"]""",
                PublishedAt = now.AddDays(-7),
                ReviewedAt = now.AddDays(-8),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-10),
            },
            // ─── Checklists (5+) ─────────────────────────────────────────
            new()
            {
                TitleEn = "Pre-Submission Document Checklist",
                TitleAr = "قائمة التحقق من المستندات قبل التقديم",
                Slug = "pre-submission-document-checklist",
                DescriptionEn = "Everything to gather and verify before you hit submit.",
                DescriptionAr = "كل ما يجب تجهيزه والتحقق منه قبل الضغط على زر التقديم.",
                ContentMarkdownEn = ResourceBodies.PreSubmitChecklistEn,
                ContentMarkdownAr = ResourceBodies.PreSubmitChecklistAr,
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
            new()
            {
                TitleEn = "Visa Interview Day Checklist",
                TitleAr = "قائمة التحقق ليوم مقابلة التأشيرة",
                Slug = "visa-interview-day-checklist",
                DescriptionEn = "What to bring, what to wear and what to say at your embassy interview.",
                DescriptionAr = "ما تأخذه معك، وما ترتديه، وما تقوله في مقابلة السفارة.",
                ContentMarkdownEn = ResourceBodies.VisaInterviewChecklistEn,
                ContentMarkdownAr = ResourceBodies.VisaInterviewChecklistAr,
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Checklist,
                Status = ResourceStatus.Published,
                CategorySlug = "visas",
                TagsJson = """["checklist","visa"]""",
                PublishedAt = now.AddDays(-11),
                ReviewedAt = now.AddDays(-12),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-14),
            },
            new()
            {
                TitleEn = "Pre-Departure Packing & Paperwork Checklist",
                TitleAr = "قائمة التحقق من التعبئة والأوراق قبل السفر",
                Slug = "pre-departure-packing-paperwork-checklist",
                DescriptionEn = "Everything to pack, certify and notify two weeks before you fly.",
                DescriptionAr = "كل ما يجب تعبئته وتوثيقه وإبلاغه قبل أسبوعين من السفر.",
                ContentMarkdownEn = ResourceBodies.PreDepartureChecklistEn,
                ContentMarkdownAr = ResourceBodies.PreDepartureChecklistAr,
                AuthorUserId = users.Consultants[2].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Checklist,
                Status = ResourceStatus.Published,
                CategorySlug = "life-abroad",
                TagsJson = """["checklist","pre-departure"]""",
                PublishedAt = now.AddDays(-9),
                ReviewedAt = now.AddDays(-10),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-12),
            },
            new()
            {
                TitleEn = "First-Week-Abroad Settling-In Checklist",
                TitleAr = "قائمة التحقق للاستقرار في الأسبوع الأول بالخارج",
                Slug = "first-week-abroad-checklist",
                DescriptionEn = "Bank, SIM, registration, transit pass — the seven errands that unlock everything else.",
                DescriptionAr = "البنك والشريحة والتسجيل وبطاقة المواصلات — سبع مهمات تفتح كل شيء آخر.",
                ContentMarkdownEn = ResourceBodies.FirstWeekChecklistEn,
                ContentMarkdownAr = ResourceBodies.FirstWeekChecklistAr,
                AuthorUserId = users.Consultants[1].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Checklist,
                Status = ResourceStatus.Published,
                CategorySlug = "life-abroad",
                TagsJson = """["checklist","life-abroad"]""",
                PublishedAt = now.AddDays(-6),
                ReviewedAt = now.AddDays(-7),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-9),
            },
            new()
            {
                TitleEn = "Language-Test Preparation Weekly Checklist",
                TitleAr = "قائمة التحقق الأسبوعية للتحضير لاختبار اللغة",
                Slug = "language-test-weekly-checklist",
                DescriptionEn = "A study cadence with weekly drills, mock tests and review milestones for IELTS or TOEFL.",
                DescriptionAr = "وتيرة دراسة بتدريبات أسبوعية واختبارات تجريبية ومراحل مراجعة لاختبار الآيلتس أو التوفل.",
                ContentMarkdownEn = ResourceBodies.LanguageChecklistEn,
                ContentMarkdownAr = ResourceBodies.LanguageChecklistAr,
                AuthorUserId = users.Consultants[0].Id,
                AuthorRole = "Consultant",
                Type = ResourceType.Checklist,
                Status = ResourceStatus.Published,
                CategorySlug = "language",
                TagsJson = """["checklist","language"]""",
                PublishedAt = now.AddDays(-4),
                ReviewedAt = now.AddDays(-5),
                ReviewedByAdminId = admin,
                CreatedAt = now.AddDays(-7),
            },
            // ─── Non-published moderation-state coverage ──────────────────
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

        await SeedResourceChaptersAsync(db, resources, logger, ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} resources covering all statuses and types", resources.Count);

        // Re-load with chapters populated so the engagement seeder can use them.
        return await db.Resources.IgnoreQueryFilters()
            .Include(r => r.Chapters)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Seeds chapter content for the multi-part guides. Idempotent on
    /// <see cref="ResourceChild"/> being empty, so it back-fills chapters even
    /// when the resources were created by an earlier seed run that predated
    /// this code. Guides are matched by slug.
    /// </summary>
    private static async Task SeedResourceChaptersAsync(
        ApplicationDbContext db, IReadOnlyList<Resource> resources, ILogger logger, CancellationToken ct)
    {
        if (await db.ResourceChapters.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var mainGuide = resources.FirstOrDefault(
            r => r.Slug == "complete-scholarship-application-guide");
        var pendingGuide = resources.FirstOrDefault(
            r => r.Slug == "budgeting-international-student");

        var chapters = new List<ResourceChild>();
        if (mainGuide is not null)
        {
            chapters.AddRange(
            [
                new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Researching scholarships", TitleAr = "البحث عن المنح", ContentMarkdownEn = "Use category filters and deadlines to build a shortlist.", ContentMarkdownAr = "استخدم فلاتر التصنيف والمواعيد لبناء قائمة مختصرة.", SortOrder = 1, EstimatedReadMinutes = 6 },
                new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Preparing your documents", TitleAr = "تجهيز مستنداتك", ContentMarkdownEn = "Gather transcripts, references and a tailored statement.", ContentMarkdownAr = "اجمع كشوف الدرجات والتوصيات وبياناً مخصصاً.", SortOrder = 2, EstimatedReadMinutes = 8 },
                new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Writing the personal statement", TitleAr = "كتابة البيان الشخصي", ContentMarkdownEn = "Tell a focused story that answers the prompt.", ContentMarkdownAr = "اروِ قصة مركزة تجيب عن السؤال.", SortOrder = 3, EstimatedReadMinutes = 10 },
                new ResourceChild { ResourceId = mainGuide.Id, TitleEn = "Submitting and following up", TitleAr = "التقديم والمتابعة", ContentMarkdownEn = "Submit early and track the application status.", ContentMarkdownAr = "قدّم مبكراً وتابع حالة الطلب.", SortOrder = 4, EstimatedReadMinutes = 5 },
            ]);
        }

        if (pendingGuide is not null)
        {
            chapters.AddRange(
            [
                new ResourceChild { ResourceId = pendingGuide.Id, TitleEn = "Fixed vs variable costs", TitleAr = "التكاليف الثابتة والمتغيرة", ContentMarkdownEn = "Separate rent and insurance from food and leisure.", ContentMarkdownAr = "افصل الإيجار والتأمين عن الطعام والترفيه.", SortOrder = 1, EstimatedReadMinutes = 5 },
                new ResourceChild { ResourceId = pendingGuide.Id, TitleEn = "Building an emergency fund", TitleAr = "بناء صندوق للطوارئ", ContentMarkdownEn = "Aim for one month of expenses in reserve.", ContentMarkdownAr = "استهدف ادخار نفقات شهر كاحتياطي.", SortOrder = 2, EstimatedReadMinutes = 4 },
            ]);
        }

        if (chapters.Count == 0)
        {
            return;
        }

        db.ResourceChapters.AddRange(chapters);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {C} resource chapters", chapters.Count);
    }

    /// <summary>
    /// Rewrites any `https://videos.scholarpath.local/...` placeholder video
    /// URLs left over from earlier seed runs to a YouTube search URL that
    /// always resolves. The mapping is keyed by slug so each video keeps a
    /// topical destination instead of a generic search.
    /// </summary>
    private static async Task BackfillVideoUrlsAsync(
        ApplicationDbContext db, IReadOnlyList<Resource> resources, ILogger logger, CancellationToken ct)
    {
        // Slug → replacement URL. Anything not in this map but starting with
        // the placeholder host falls back to a generic scholarship-help search.
        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["webinar-interview-prep-masterclass"] = "https://www.youtube.com/results?search_query=scholarship+interview+preparation+masterclass",
            ["webinar-standout-statement-of-purpose"] = "https://www.youtube.com/results?search_query=how+to+write+statement+of+purpose+masters",
            ["workshop-ielts-speaking-practice"] = "https://www.youtube.com/results?search_query=IELTS+speaking+practice+with+feedback",
            ["panel-life-in-germany"] = "https://www.youtube.com/results?search_query=studying+in+germany+as+international+student",
            ["webinar-funding-masters-multiple-sources"] = "https://www.youtube.com/results?search_query=how+to+fund+masters+degree+scholarships",
            ["walkthrough-personal-statement-live"] = "https://www.youtube.com/results?search_query=personal+statement+writing+walkthrough",
        };

        var fallback = "https://www.youtube.com/results?search_query=scholarship+application+help";
        var updated = 0;

        foreach (var resource in resources)
        {
            if (string.IsNullOrEmpty(resource.ExternalLinkUrl)) continue;
            if (!resource.ExternalLinkUrl.Contains("videos.scholarpath.local", StringComparison.OrdinalIgnoreCase))
                continue;

            resource.ExternalLinkUrl = replacements.TryGetValue(resource.Slug, out var mapped)
                ? mapped
                : fallback;
            updated++;
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            logger.LogInformation(
                "Back-filled {N} resource video URLs from videos.scholarpath.local placeholders", updated);
        }
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
