using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the expertise-tag lookup that drives the consultant marketplace
    /// filter chips. Idempotent on the table being empty.
    /// </summary>
    private static async Task SeedExpertiseTagsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        if (await db.ExpertiseTags.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        db.ExpertiseTags.AddRange(
            new ExpertiseTag { NameEn = "Statement of Purpose", NameAr = "خطاب الغرض", Slug = "statement-of-purpose", Category = "Application" },
            new ExpertiseTag { NameEn = "Interview Preparation", NameAr = "التحضير للمقابلة", Slug = "interview-prep", Category = "Application" },
            new ExpertiseTag { NameEn = "University Selection", NameAr = "اختيار الجامعة", Slug = "university-selection", Category = "Planning" },
            new ExpertiseTag { NameEn = "CV Review", NameAr = "مراجعة السيرة الذاتية", Slug = "cv-review", Category = "Application" },
            new ExpertiseTag { NameEn = "Research Proposals", NameAr = "مقترحات البحث", Slug = "research-proposals", Category = "Research" },
            new ExpertiseTag { NameEn = "Funding Strategy", NameAr = "استراتيجية التمويل", Slug = "funding-strategy", Category = "Planning" },
            new ExpertiseTag { NameEn = "PhD Applications", NameAr = "طلبات الدكتوراه", Slug = "phd-applications", Category = "Application" },
            new ExpertiseTag { NameEn = "Scholarship Search", NameAr = "البحث عن المنح", Slug = "scholarship-search", Category = "Planning" });
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} expertise tags", 8);
    }

    /// <summary>
    /// Seeds role-upgrade requests covering EVERY <see cref="UpgradeRequestStatus"/>
    /// (Pending / Approved / Rejected / Cancelled), each with a file + link
    /// attachment, so the admin upgrade-review queue has data.
    /// </summary>
    private static async Task SeedUpgradeRequestsAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.UpgradeRequests.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var admin = users.PrimaryAdmin.Id;

        var pending = new UpgradeRequest
        {
            UserId = users.Students[3].Id,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Pending,
            Reason = "I have five years of admissions-consulting experience and would like to offer paid sessions.",
            CreatedAt = now.AddDays(-4),
        };
        var approved = new UpgradeRequest
        {
            UserId = users.Students[1].Id,
            Target = UpgradeTarget.Company,
            Status = UpgradeRequestStatus.Approved,
            Reason = "Registering our education non-profit to publish scholarships.",
            ReviewerNotes = "Registration documents verified. Approved.",
            ReviewedByAdminId = admin,
            ReviewedAt = now.AddDays(-20),
            CreatedAt = now.AddDays(-25),
        };
        var rejected = new UpgradeRequest
        {
            UserId = users.Students[2].Id,
            Target = UpgradeTarget.Consultant,
            Status = UpgradeRequestStatus.Rejected,
            Reason = "I want to mentor students.",
            ReviewerNotes = "Insufficient evidence of professional consulting experience. Please reapply with references.",
            ReviewedByAdminId = admin,
            ReviewedAt = now.AddDays(-15),
            CreatedAt = now.AddDays(-18),
        };
        var cancelled = new UpgradeRequest
        {
            UserId = users.Students[4].Id,
            Target = UpgradeTarget.Company,
            Status = UpgradeRequestStatus.Cancelled,
            Reason = "Submitted by mistake.",
            CreatedAt = now.AddDays(-30),
        };

        db.UpgradeRequests.AddRange(pending, approved, rejected, cancelled);

        db.UpgradeRequestFiles.AddRange(
            new UpgradeRequestFile { UpgradeRequestId = pending.Id, FileName = "consulting-cv.pdf", BlobUrl = "https://demo.blob/upgrades/consulting-cv.pdf", ContentType = "application/pdf", SizeBytes = 184_320, UploadedAt = now.AddDays(-4) },
            new UpgradeRequestFile { UpgradeRequestId = approved.Id, FileName = "company-registration.pdf", BlobUrl = "https://demo.blob/upgrades/company-registration.pdf", ContentType = "application/pdf", SizeBytes = 256_000, UploadedAt = now.AddDays(-25) },
            new UpgradeRequestFile { UpgradeRequestId = rejected.Id, FileName = "intro-letter.pdf", BlobUrl = "https://demo.blob/upgrades/intro-letter.pdf", ContentType = "application/pdf", SizeBytes = 64_000, UploadedAt = now.AddDays(-18) });

        db.UpgradeRequestLinks.AddRange(
            new UpgradeRequestLink { UpgradeRequestId = pending.Id, Label = "LinkedIn profile", Url = "https://www.linkedin.com/in/demo-consultant" },
            new UpgradeRequestLink { UpgradeRequestId = approved.Id, Label = "Organisation website", Url = "https://www.demo-education.org" });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} upgrade requests (all statuses)", 4);
    }

    /// <summary>
    /// Seeds a broad scholarship catalogue: every <see cref="ScholarshipStatus"/>
    /// (Draft / Open / Closed / Archived / UnderReview), both
    /// <see cref="ListingMode"/>s, a spread of <see cref="FundingType"/> and
    /// <see cref="AcademicLevel"/>, several featured rows, owned by the seeded
    /// companies (one admin-curated external listing has no owner). Each gets
    /// <see cref="ScholarshipChild"/> requirement / benefit rows.
    /// </summary>
    private static async Task<IReadOnlyList<Scholarship>> SeedScholarshipsAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<Category> categories,
        ILogger logger, CancellationToken ct)
    {
        var existing = await db.Scholarships.ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;
        Category Cat(string slug) => categories.First(c => c.Slug == slug);
        var stem = Cat("stem");
        var arts = Cat("arts-humanities");
        var business = Cat("business");
        var medical = Cat("medical");

        var globalScholars = users.Companies[0];
        var futureFund = users.Companies[1];
        var nileBridge = users.Companies[2];

        var list = new List<Scholarship>
        {
            // 1) OPEN, in-app, fully funded, featured #1
            new()
            {
                TitleEn = "Global Scholars STEM Excellence Award",
                TitleAr = "جائزة التميز في العلوم والتكنولوجيا",
                DescriptionEn = "A fully funded Master's scholarship for outstanding STEM students, covering tuition, a monthly stipend, and travel.",
                DescriptionAr = "منحة ماجستير ممولة بالكامل للطلاب المتميزين في مجالات العلوم والتكنولوجيا، تغطي الرسوم الدراسية وراتباً شهرياً وتذاكر السفر.",
                Slug = "global-scholars-stem-excellence",
                CategoryId = stem.Id,
                OwnerCompanyId = globalScholars.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(45),
                OpenedAt = now.AddDays(-15),
                IsFeatured = true,
                FeaturedOrder = 1,
                FundingType = FundingType.FullyFunded,
                FundingAmountUsd = 48_000m,
                TargetLevel = AcademicLevel.Masters,
                TargetCountriesJson = """["US","GB","CA"]""",
                EligibilityRequirementsEn = "GPA 3.5+, a STEM Bachelor's degree, and proof of English proficiency.",
                EligibilityRequirementsAr = "معدل تراكمي 3.5 فأعلى، شهادة بكالوريوس في تخصص علمي، وإثبات إتقان اللغة الإنجليزية.",
                TagsJson = """["fully-funded","masters","stem"]""",
                ReviewFeeUsd = 25m,
                CreatedAt = now.AddDays(-16),
            },
            // 2) OPEN, external URL listing, partially funded, featured #2
            new()
            {
                TitleEn = "FutureFund Undergraduate Bridge Grant",
                TitleAr = "منحة جسر المرحلة الجامعية من فيوتشر فند",
                DescriptionEn = "A partial tuition grant for first-year undergraduates in any discipline. Applications are handled on the foundation's own portal.",
                DescriptionAr = "منحة جزئية للرسوم الدراسية لطلاب السنة الأولى الجامعية في أي تخصص. تتم معالجة الطلبات على بوابة المؤسسة الخاصة.",
                Slug = "futurefund-undergraduate-bridge-grant",
                CategoryId = business.Id,
                OwnerCompanyId = futureFund.Id,
                Mode = ListingMode.ExternalUrl,
                ExternalApplicationUrl = "https://apply.futurefund.org/bridge-grant",
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(30),
                OpenedAt = now.AddDays(-10),
                IsFeatured = true,
                FeaturedOrder = 2,
                FundingType = FundingType.PartiallyFunded,
                FundingAmountUsd = 6_000m,
                TargetLevel = AcademicLevel.Undergrad,
                TargetCountriesJson = """["US"]""",
                EligibilityRequirementsEn = "Enrolled first-year undergraduate, demonstrated financial need.",
                EligibilityRequirementsAr = "طالب مسجل في السنة الأولى الجامعية، مع إثبات الحاجة المالية.",
                TagsJson = """["partial","undergrad"]""",
                CreatedAt = now.AddDays(-11),
            },
            // 3) OPEN, in-app, PhD, tuition only, featured #3
            new()
            {
                TitleEn = "Nile Bridge Doctoral Research Fellowship",
                TitleAr = "زمالة نايل بريدج للبحث في مرحلة الدكتوراه",
                DescriptionEn = "A doctoral fellowship covering full tuition for PhD candidates in medical and life sciences.",
                DescriptionAr = "زمالة دكتوراه تغطي الرسوم الدراسية كاملةً لطلاب الدكتوراه في العلوم الطبية وعلوم الحياة.",
                Slug = "nile-bridge-doctoral-research-fellowship",
                CategoryId = medical.Id,
                OwnerCompanyId = nileBridge.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(60),
                OpenedAt = now.AddDays(-20),
                IsFeatured = true,
                FeaturedOrder = 3,
                FundingType = FundingType.TuitionOnly,
                FundingAmountUsd = 22_000m,
                TargetLevel = AcademicLevel.PhD,
                TargetCountriesJson = """["EG","JO","AE"]""",
                EligibilityRequirementsEn = "A relevant Master's degree and an accepted research proposal.",
                EligibilityRequirementsAr = "شهادة ماجستير في تخصص ذي صلة ومقترح بحثي مقبول.",
                TagsJson = """["phd","tuition-only","medical"]""",
                ReviewFeeUsd = 40m,
                CreatedAt = now.AddDays(-21),
            },
            // 4) OPEN, in-app, stipend only, undergrad arts
            new()
            {
                TitleEn = "Creative Arts Living Stipend",
                TitleAr = "راتب معيشة للفنون الإبداعية",
                DescriptionEn = "A monthly living stipend for undergraduate students of the visual and performing arts.",
                DescriptionAr = "راتب معيشة شهري لطلاب البكالوريوس في الفنون البصرية والأدائية.",
                Slug = "creative-arts-living-stipend",
                CategoryId = arts.Id,
                OwnerCompanyId = globalScholars.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(25),
                OpenedAt = now.AddDays(-8),
                FundingType = FundingType.StipendOnly,
                FundingAmountUsd = 9_600m,
                TargetLevel = AcademicLevel.Undergrad,
                TargetCountriesJson = """["GB","FR"]""",
                EligibilityRequirementsEn = "An enrolled arts undergraduate with a portfolio.",
                EligibilityRequirementsAr = "طالب بكالوريوس مسجل في الفنون ولديه ملف أعمال.",
                TagsJson = """["stipend","arts","undergrad"]""",
                ReviewFeeUsd = 15m,
                CreatedAt = now.AddDays(-9),
            },
            // 5) DRAFT, in-app, not yet published
            new()
            {
                TitleEn = "Women in Engineering Scholarship (Draft)",
                TitleAr = "منحة المرأة في الهندسة (مسودة)",
                DescriptionEn = "A scholarship for women pursuing engineering degrees. Still being prepared by the company.",
                DescriptionAr = "منحة للنساء الراغبات في دراسة الهندسة. لا تزال قيد الإعداد من قبل الشركة.",
                Slug = "women-in-engineering-draft",
                CategoryId = stem.Id,
                OwnerCompanyId = futureFund.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Draft,
                Deadline = now.AddDays(90),
                FundingType = FundingType.PartiallyFunded,
                FundingAmountUsd = 12_000m,
                TargetLevel = AcademicLevel.Undergrad,
                TagsJson = """["draft","engineering"]""",
                CreatedAt = now.AddDays(-3),
            },
            // 6) UNDER REVIEW, in-app — awaiting admin moderation
            new()
            {
                TitleEn = "Horizon Business Leaders Grant",
                TitleAr = "منحة قادة الأعمال من هورايزون",
                DescriptionEn = "An MBA grant submitted for admin review before publication.",
                DescriptionAr = "منحة ماجستير إدارة أعمال مقدمة لمراجعة الإدارة قبل النشر.",
                Slug = "horizon-business-leaders-grant",
                CategoryId = business.Id,
                OwnerCompanyId = nileBridge.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.UnderReview,
                Deadline = now.AddDays(70),
                FundingType = FundingType.FullyFunded,
                FundingAmountUsd = 55_000m,
                TargetLevel = AcademicLevel.Masters,
                TargetCountriesJson = """["AE","SA"]""",
                EligibilityRequirementsEn = "Two years of work experience and a Bachelor's degree.",
                EligibilityRequirementsAr = "سنتان من الخبرة العملية وشهادة بكالوريوس.",
                TagsJson = """["mba","under-review"]""",
                ReviewFeeUsd = 30m,
                CreatedAt = now.AddDays(-5),
            },
            // 7) CLOSED, in-app — deadline passed
            new()
            {
                TitleEn = "High School STEM Starter Award",
                TitleAr = "جائزة بداية العلوم لطلاب الثانوية",
                DescriptionEn = "An award for high-school students entering STEM fields. Applications are now closed.",
                DescriptionAr = "جائزة لطلاب المرحلة الثانوية المتجهين لمجالات العلوم. باب التقديم مغلق الآن.",
                Slug = "high-school-stem-starter-award",
                CategoryId = stem.Id,
                OwnerCompanyId = globalScholars.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Closed,
                Deadline = now.AddDays(-10),
                OpenedAt = now.AddDays(-70),
                FundingType = FundingType.Other,
                FundingAmountUsd = 3_000m,
                TargetLevel = AcademicLevel.HighSchool,
                TagsJson = """["high-school","closed"]""",
                ReviewFeeUsd = 10m,
                CreatedAt = now.AddDays(-72),
            },
            // 8) ARCHIVED, in-app — historical
            new()
            {
                TitleEn = "Legacy Humanities Grant 2024",
                TitleAr = "منحة الإنسانيات القديمة 2024",
                DescriptionEn = "A historical humanities grant kept for archival reference.",
                DescriptionAr = "منحة إنسانيات تاريخية محفوظة للرجوع الأرشيفي.",
                Slug = "legacy-humanities-grant-2024",
                CategoryId = arts.Id,
                OwnerCompanyId = futureFund.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Archived,
                Deadline = now.AddDays(-200),
                OpenedAt = now.AddDays(-365),
                ArchivedAt = now.AddDays(-120),
                FundingType = FundingType.PartiallyFunded,
                FundingAmountUsd = 7_500m,
                TargetLevel = AcademicLevel.Masters,
                TagsJson = """["archived","humanities"]""",
                CreatedAt = now.AddDays(-366),
            },
            // 9) OPEN, external URL — admin-curated, NO owner company, PostDoc
            new()
            {
                TitleEn = "International PostDoc Mobility Programme",
                TitleAr = "برنامج التنقل لما بعد الدكتوراه الدولي",
                DescriptionEn = "An admin-curated external listing for a PostDoc mobility programme. Apply on the official portal.",
                DescriptionAr = "إعلان خارجي منسق من الإدارة لبرنامج تنقل لما بعد الدكتوراه. قدّم عبر البوابة الرسمية.",
                Slug = "international-postdoc-mobility-programme",
                CategoryId = stem.Id,
                OwnerCompanyId = null,
                CreatedByAdminId = users.PrimaryAdmin.Id,
                Mode = ListingMode.ExternalUrl,
                ExternalApplicationUrl = "https://global-postdoc.example.org/apply",
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(80),
                OpenedAt = now.AddDays(-5),
                FundingType = FundingType.FullyFunded,
                FundingAmountUsd = 60_000m,
                TargetLevel = AcademicLevel.PostDoc,
                TargetCountriesJson = """["DE","SE","NL"]""",
                EligibilityRequirementsEn = "A PhD awarded within the last five years.",
                EligibilityRequirementsAr = "شهادة دكتوراه ممنوحة خلال السنوات الخمس الماضية.",
                TagsJson = """["postdoc","external","mobility"]""",
                CreatedAt = now.AddDays(-6),
            },
            // 10) OPEN, in-app, business undergrad — extra volume
            new()
            {
                TitleEn = "Young Entrepreneurs Tuition Scholarship",
                TitleAr = "منحة رواد الأعمال الشباب للرسوم الدراسية",
                DescriptionEn = "A tuition scholarship for undergraduate students with an entrepreneurial project.",
                DescriptionAr = "منحة رسوم دراسية لطلاب البكالوريوس أصحاب المشاريع الريادية.",
                Slug = "young-entrepreneurs-tuition-scholarship",
                CategoryId = business.Id,
                OwnerCompanyId = nileBridge.Id,
                Mode = ListingMode.InApp,
                Status = ScholarshipStatus.Open,
                Deadline = now.AddDays(38),
                OpenedAt = now.AddDays(-12),
                FundingType = FundingType.TuitionOnly,
                FundingAmountUsd = 14_000m,
                TargetLevel = AcademicLevel.Undergrad,
                TargetCountriesJson = """["EG","JO"]""",
                EligibilityRequirementsEn = "A registered or prototype-stage startup idea.",
                EligibilityRequirementsAr = "فكرة شركة ناشئة مسجلة أو في مرحلة النموذج الأولي.",
                TagsJson = """["business","tuition-only","undergrad"]""",
                ReviewFeeUsd = 20m,
                CreatedAt = now.AddDays(-13),
            },
        };

        // Shared in-app application form schema + required docs for InApp listings.
        const string formSchema = """{"fields":[{"key":"motivation","label":"Motivation statement","type":"textarea","required":true},{"key":"gpa","label":"Current GPA","type":"number","required":true}]}""";
        const string requiredDocs = """["Transcript","RecommendationLetter","PersonalStatement"]""";
        foreach (var s in list.Where(s => s.Mode == ListingMode.InApp))
        {
            s.ApplicationFormSchemaJson = formSchema;
            s.RequiredDocumentsJson = requiredDocs;
        }

        db.Scholarships.AddRange(list);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Children: requirement + benefit rows for each scholarship.
        var children = new List<ScholarshipChild>();
        foreach (var s in list)
        {
            children.Add(new ScholarshipChild { ScholarshipId = s.Id, ChildType = "Requirement", KeyEn = "Academic transcript", KeyAr = "كشف الدرجات الأكاديمي", SortOrder = 1 });
            children.Add(new ScholarshipChild { ScholarshipId = s.Id, ChildType = "Requirement", KeyEn = "Letter of recommendation", KeyAr = "خطاب توصية", SortOrder = 2 });
            children.Add(new ScholarshipChild { ScholarshipId = s.Id, ChildType = "Benefit", KeyEn = "Tuition coverage", KeyAr = "تغطية الرسوم الدراسية", ValueEn = "Up to 100%", ValueAr = "حتى 100%", SortOrder = 1 });
            children.Add(new ScholarshipChild { ScholarshipId = s.Id, ChildType = "Benefit", KeyEn = "Mentorship", KeyAr = "إرشاد أكاديمي", ValueEn = "Assigned mentor for one year", ValueAr = "مرشد معيّن لمدة عام", SortOrder = 2 });
        }

        db.ScholarshipChildren.AddRange(children);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} scholarships (+{C} child rows) covering all statuses and modes", list.Count, children.Count);
        return list;
    }

    /// <summary>
    /// Seeds <see cref="SavedScholarship"/> bookmarks linking demo students to
    /// scholarships, with a couple of personal notes. Idempotent on the table
    /// being empty; the unique <c>(UserId, ScholarshipId)</c> index is respected
    /// by using distinct pairs.
    /// </summary>
    private static async Task SeedSavedScholarshipsAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<Scholarship> scholarships,
        ILogger logger, CancellationToken ct)
    {
        if (await db.SavedScholarships.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var open = scholarships.Where(s => s.Status == ScholarshipStatus.Open).ToList();
        if (open.Count == 0)
        {
            return;
        }

        var saved = new List<SavedScholarship>
        {
            new() { UserId = users.Students[0].Id, ScholarshipId = open[0].Id, SavedAt = now.AddDays(-6), Note = "Strong match — start the application this week." },
            new() { UserId = users.Students[0].Id, ScholarshipId = open[Math.Min(2, open.Count - 1)].Id, SavedAt = now.AddDays(-5) },
            new() { UserId = users.Students[1].Id, ScholarshipId = open[0].Id, SavedAt = now.AddDays(-4) },
            new() { UserId = users.Students[1].Id, ScholarshipId = open[1].Id, SavedAt = now.AddDays(-3), Note = "Check the external portal deadline." },
            new() { UserId = users.Students[2].Id, ScholarshipId = open[Math.Min(3, open.Count - 1)].Id, SavedAt = now.AddDays(-2) },
        };

        db.SavedScholarships.AddRange(saved.DistinctBy(s => (s.UserId, s.ScholarshipId)));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} saved scholarships", saved.Count);
    }
}
