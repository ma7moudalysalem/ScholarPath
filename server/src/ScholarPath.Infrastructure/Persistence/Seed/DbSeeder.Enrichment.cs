using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Deep "as if the platform has run for a year" enrichment: a large bilingual
/// body of community threads + replies + votes + flags (with real moderation
/// scenarios), a populated bilingual document vault, a year of audit-log
/// activity, and the admin review queues (low-rating flags). Runs after the
/// base seeders; every section is independently idempotent.
/// </summary>
public static partial class DbSeeder
{
    // ── Bilingual community content pools ────────────────────────────────────
    private static readonly (string TitleEn, string TitleAr, string BodyEn, string BodyAr)[] ThreadPool =
    [
        ("How early should I start my scholarship applications?", "قبل بكام من الوقت أبدأ في تقديمات المنح؟",
         "I keep hearing 'start early' but what does that actually mean in months?", "بسمع دايمًا «ابدأ بدري» بس ده يعني كام شهر بالظبط؟"),
        ("Best way to ask a professor for a recommendation letter", "أفضل طريقة أطلب بيها خطاب توصية من دكتور",
         "Do I email or ask in person? And how much notice should I give?", "أبعت إيميل ولا أطلب وجهًا لوجه؟ وأديله مهلة قد إيه؟"),
        ("Fully-funded PhD in Germany — is the DAAD worth the effort?", "دكتوراه ممولة بالكامل في ألمانيا — هل الـ DAAD تستاهل؟",
         "The application looks huge. Anyone got it and can share tips?", "التقديم شكله ضخم. حد خده ويقدر يشارك نصايح؟"),
        ("IELTS 7.0 in one month — my study plan", "آيلتس 7.0 في شهر — خطة مذاكرتي",
         "Sharing the exact daily routine that worked for me.", "بشارك الروتين اليومي اللي نفع معايا بالظبط."),
        ("Statement of purpose: how personal is too personal?", "خطاب الغرض: إمتى يبقى شخصي أكتر من اللازم؟",
         "I want to stand out but not overshare. Where's the line?", "عايز أتميز من غير مبالغة. فين الخط الفاصل؟"),
        ("Should I accept a partial offer or wait for a full one?", "أقبل عرض جزئي ولا أستنى الكامل؟",
         "Deadline to accept is in 5 days and the other result isn't out.", "آخر ميعاد للقبول بعد 5 أيام والنتيجة التانية لسه."),
        ("Visa interview — what they actually ask", "مقابلة التأشيرة — بيسألوا إيه فعلًا",
         "Just finished mine. Posting the real questions I got.", "خلّصت مقابلتي دلوقتي. بكتب الأسئلة الحقيقية اللي جتلي."),
        ("Cheapest safe cities for international students", "أرخص مدن آمنة للطلاب الدوليين",
         "Trying to budget realistically. Where did rent surprise you?", "بحاول أعمل ميزانية واقعية. الإيجار فاجأك فين؟"),
        ("Do scholarships really check social media?", "هل المنح بتشوف السوشيال ميديا فعلًا؟",
         "A friend said committees google you. Is that a real thing?", "صاحبي قال اللجان بتعمل جوجل لاسمك. ده حقيقي؟"),
        ("My timeline from application to acceptance (9 months)", "الخط الزمني من التقديم للقبول (9 شهور)",
         "For anyone anxious about the wait — here's how mine went.", "لأي حد قلقان من الانتظار — دي رحلتي كاملة."),
        ("Reusing one personal statement across scholarships", "استخدام بيان شخصي واحد لأكتر من منحة",
         "Is tailoring each one really necessary, or can I template it?", "لازم أفصّل واحد لكل منحة، ولا أعمل قالب؟"),
        ("Funding gaps: how did you cover the first month abroad?", "فجوات التمويل: غطّيت أول شهر بره إزاي؟",
         "Stipends often arrive late. What bridged the gap for you?", "الرواتب بتتأخر غالبًا. إيه اللي غطّى الفرق عندك؟"),
        ("Canada vs UK for a Master's in Data Science", "كندا ولا بريطانيا لماجستير علم البيانات",
         "Cost, work rights, and PR paths — how do they compare?", "التكلفة وحق العمل ومسارات الإقامة — إيه الفرق؟"),
        ("Recommendation letters: quantity vs quality", "خطابات التوصية: العدد ولا الجودة",
         "Three strong or five average? What actually moves the needle?", "تلاتة أقوياء ولا خمسة عاديين؟ إيه اللي بيفرق فعلًا؟"),
        ("First-generation applicant — imposter syndrome is real", "أول جيل يقدّم — الإحساس بعدم الاستحقاق حقيقي",
         "Anyone else feel like they don't belong in these rooms?", "حد تاني حاسس إنه مش مكانه في الأماكن دي؟"),
    ];

    private static readonly string[] ReplyEn =
    [
        "This is exactly what I needed, thank you for writing it up.",
        "Start at least 3 months before the deadline — documents take forever.",
        "I did this last cycle and it worked. Happy to share my draft.",
        "Careful with the 28-day financial rule, it caught me off guard.",
        "Great breakdown. Bookmarking for my own applications.",
        "Ask in person if you can, then follow up with an email summary.",
        "I disagree slightly — quality of the letter beats quantity every time.",
        "Which country are you targeting? The answer really depends on that.",
        "Congrats! This gives me hope while I wait on my own results.",
        "Templates are fine for the body, but always tailor the opening.",
    ];

    private static readonly string[] ReplyAr =
    [
        "ده بالظبط اللي كنت محتاجه، شكرًا إنك كتبته.",
        "ابدأ على الأقل 3 شهور قبل الميعاد — المستندات بتاخد وقت طويل.",
        "عملت كده السنة اللي فاتت ونفع. أقدر أشارك مسودتي بكل سرور.",
        "خلي بالك من قاعدة الـ 28 يوم المالية، دي فاجأتني.",
        "شرح ممتاز. بحفظه لتقديماتي.",
        "اطلب وجهًا لوجه لو تقدر، وبعدها ابعت إيميل بالتلخيص.",
        "مختلف معاك شوية — جودة الخطاب أهم من العدد دايمًا.",
        "بتستهدف أنهي دولة؟ الإجابة بتعتمد على ده فعلًا.",
        "مبروك! ده بيديني أمل وأنا مستني نتيجتي.",
        "القوالب كويسة للنص، بس دايمًا فصّل المقدمة.",
    ];

    private static async Task SeedEnrichmentAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        var rng = new Random(77);
        var now = DateTimeOffset.UtcNow;

        // Resolve broad participant pools by role (bulk generated users included).
        async Task<List<Guid>> IdsInRoleAsync(string role, int take) =>
            await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id
                   where r.Name == role select ur.UserId).Distinct().Take(take)
                .ToListAsync(ct).ConfigureAwait(false);

        var studentIds = await IdsInRoleAsync("Student", 800).ConfigureAwait(false);
        if (studentIds.Count == 0) return;

        await EnrichCommunityAsync(db, studentIds, rng, now, logger, ct).ConfigureAwait(false);
        await EnrichDocumentsAsync(db, studentIds, rng, now, logger, ct).ConfigureAwait(false);
        await EnrichAuditLogAsync(db, studentIds, rng, now, logger, ct).ConfigureAwait(false);
        await EnrichAdminQueuesAsync(db, logger, ct).ConfigureAwait(false);
    }

    private static async Task EnrichCommunityAsync(
        ApplicationDbContext db, List<Guid> studentIds, Random rng, DateTimeOffset now,
        ILogger logger, CancellationToken ct)
    {
        // Idempotent: base seeder makes ~30 posts; skip once enriched.
        if (await db.ForumPosts.IgnoreQueryFilters().CountAsync(ct).ConfigureAwait(false) > 120)
            return;

        var categoryIds = await db.ForumCategories.Select(c => c.Id).ToListAsync(ct).ConfigureAwait(false);
        if (categoryIds.Count == 0) return;

        // ── Root threads spread across the last ~12 months ──
        var roots = new List<ForumPost>();
        for (var i = 0; i < 90; i++)
        {
            var t = ThreadPool[rng.Next(ThreadPool.Length)];
            var daysAgo = rng.Next(3, 360);
            var post = new ForumPost
            {
                Id = Guid.NewGuid(),
                AuthorId = studentIds[rng.Next(studentIds.Count)],
                CategoryId = categoryIds[rng.Next(categoryIds.Count)],
                Title = t.TitleEn,
                BodyMarkdown = t.BodyEn,
                TitleEn = t.TitleEn,
                TitleAr = t.TitleAr,
                BodyEn = t.BodyEn,
                BodyAr = t.BodyAr,
                ModerationStatus = PostModerationStatus.Visible,
                CreatedAt = now.AddDays(-daysAgo).AddHours(-rng.Next(24)),
            };
            roots.Add(post);
        }
        db.ForumPosts.AddRange(roots);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        db.ChangeTracker.Clear();

        // ── Replies (the "comments"), bilingual, 1–9 per thread ──
        var replies = new List<ForumPost>();
        var votes = new List<ForumVote>();
        var replyCountByRoot = new Dictionary<Guid, int>();
        foreach (var root in roots)
        {
            var n = rng.Next(1, 10);
            replyCountByRoot[root.Id] = n;
            for (var k = 0; k < n; k++)
            {
                var ar = rng.Next(2) == 0;
                var body = ar ? ReplyAr[rng.Next(ReplyAr.Length)] : ReplyEn[rng.Next(ReplyEn.Length)];
                replies.Add(new ForumPost
                {
                    Id = Guid.NewGuid(),
                    AuthorId = studentIds[rng.Next(studentIds.Count)],
                    ParentPostId = root.Id,
                    BodyMarkdown = body,
                    BodyEn = ar ? null : body,
                    BodyAr = ar ? body : null,
                    ModerationStatus = PostModerationStatus.Visible,
                    CreatedAt = root.CreatedAt.AddHours(rng.Next(2, 240)),
                });
            }
        }
        // Persist replies in batches.
        for (var i = 0; i < replies.Count; i += 250)
        {
            db.ForumPosts.AddRange(replies.GetRange(i, Math.Min(250, replies.Count - i)));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }

        // ── Votes on roots + a couple of flags/moderation scenarios ──
        var flags = new List<ForumFlag>();
        var moderationUpdates = new List<(Guid id, int up, int down, int reply, int flag, bool autoHide, PostModerationStatus status)>();
        var reasons = new[] { "spam", "harassment", "off-topic", "misinformation", "inappropriate" };
        for (var idx = 0; idx < roots.Count; idx++)
        {
            var root = roots[idx];
            var up = rng.Next(0, 40);
            var down = rng.Next(0, 6);
            // The unique (ForumPostId, UserId) index means one vote per user per
            // post — sample DISTINCT voters (capped) so we never collide.
            var distinctVoters = new HashSet<Guid>();
            var wantVoters = Math.Min(up + down, 20);
            var guard = 0;
            while (distinctVoters.Count < wantVoters && guard++ < wantVoters * 4)
            {
                distinctVoters.Add(studentIds[rng.Next(studentIds.Count)]);
            }
            var voterList = distinctVoters.ToList();
            for (var v = 0; v < voterList.Count; v++)
            {
                votes.Add(new ForumVote
                {
                    ForumPostId = root.Id,
                    UserId = voterList[v],
                    VoteType = v < up ? VoteType.Up : VoteType.Down,
                    VotedAt = root.CreatedAt.AddHours(rng.Next(1, 200)),
                });
            }

            var status = PostModerationStatus.Visible;
            var autoHide = false;
            var flagCount = 0;
            // ~1 in 12 threads draws flags → some auto-hidden (queue), some pending.
            if (idx % 12 == 5)
            {
                flagCount = 3; autoHide = true; status = PostModerationStatus.Hidden;
                for (var f = 0; f < 3; f++)
                    flags.Add(new ForumFlag { ForumPostId = root.Id, FlaggedByUserId = studentIds[rng.Next(studentIds.Count)], Reason = reasons[rng.Next(reasons.Length)], FlaggedAt = root.CreatedAt.AddHours(rng.Next(2, 100)), IsValid = true });
            }
            else if (idx % 12 == 9)
            {
                flagCount = 1; status = PostModerationStatus.PendingReview;
                flags.Add(new ForumFlag { ForumPostId = root.Id, FlaggedByUserId = studentIds[rng.Next(studentIds.Count)], Reason = reasons[rng.Next(reasons.Length)], AdditionalDetails = "Please review this thread.", FlaggedAt = root.CreatedAt.AddHours(rng.Next(2, 100)), IsValid = true });
            }

            moderationUpdates.Add((root.Id, up, down, replyCountByRoot[root.Id], flagCount, autoHide, status));
        }

        for (var i = 0; i < votes.Count; i += 500)
        {
            db.ForumVotes.AddRange(votes.GetRange(i, Math.Min(500, votes.Count - i)));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }
        if (flags.Count > 0)
        {
            db.ForumFlags.AddRange(flags);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }

        // Stamp denormalised counts + moderation state via tracked entities
        // (only ~90 roots — trivial memory, and it handles nullable AutoHiddenAt
        // natively instead of fighting raw-SQL DBNull mapping).
        var modById = moderationUpdates.ToDictionary(m => m.id);
        var rootIds = roots.Select(r => r.Id).ToList();
        var rootEntities = await db.ForumPosts
            .Where(p => rootIds.Contains(p.Id))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var p in rootEntities)
        {
            if (!modById.TryGetValue(p.Id, out var u)) continue;
            p.UpvoteCount = u.up;
            p.DownvoteCount = u.down;
            p.ReplyCount = u.reply;
            p.FlagCount = u.flag;
            p.IsAutoHidden = u.autoHide;
            p.AutoHiddenAt = u.autoHide ? now.AddDays(-rng.Next(1, 120)) : null;
            p.ModerationStatus = u.status;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        db.ChangeTracker.Clear();

        logger.LogInformation(
            "Enriched community: {R} threads, {C} replies, {V} votes, {F} flags (incl. moderation queue).",
            roots.Count, replies.Count, votes.Count, flags.Count);
    }

    private static readonly (string En, string Ar, DocumentCategory Cat)[] DocPool =
    [
        ("Academic Transcript.pdf", "كشف الدرجات الأكاديمي.pdf", DocumentCategory.Transcript),
        ("Bachelor Degree Certificate.pdf", "شهادة البكالوريوس.pdf", DocumentCategory.Certificate),
        ("Recommendation Letter - Prof Ahmed.pdf", "خطاب توصية - د. أحمد.pdf", DocumentCategory.RecommendationLetter),
        ("Personal Statement (final).pdf", "البيان الشخصي (النهائي).pdf", DocumentCategory.PersonalStatement),
        ("CV - Updated.pdf", "السيرة الذاتية - محدّثة.pdf", DocumentCategory.Resume),
        ("Passport Copy.pdf", "صورة جواز السفر.pdf", DocumentCategory.IdentityDocument),
        ("IELTS Score Report.pdf", "تقرير درجات الآيلتس.pdf", DocumentCategory.ProofOfEnglish),
        ("Bank Statement.pdf", "كشف حساب بنكي.pdf", DocumentCategory.FinancialDocument),
        ("Statement of Purpose.pdf", "خطاب الغرض.pdf", DocumentCategory.PersonalStatement),
        ("TOEFL Certificate.pdf", "شهادة التوفل.pdf", DocumentCategory.ProofOfEnglish),
        ("Research Proposal.pdf", "مقترح البحث.pdf", DocumentCategory.Other),
        ("National ID.jpg", "بطاقة الرقم القومي.jpg", DocumentCategory.IdentityDocument),
    ];

    private static async Task EnrichDocumentsAsync(
        ApplicationDbContext db, List<Guid> studentIds, Random rng, DateTimeOffset now,
        ILogger logger, CancellationToken ct)
    {
        if (await db.Documents.IgnoreQueryFilters().CountAsync(ct).ConfigureAwait(false) > 60)
            return;

        var docs = new List<Document>();
        // ~4 documents each for a slice of students → a populated vault.
        foreach (var studentId in studentIds.Take(120))
        {
            var count = rng.Next(2, 7);
            for (var k = 0; k < count; k++)
            {
                var d = DocPool[rng.Next(DocPool.Length)];
                var ar = rng.Next(2) == 0;
                docs.Add(new Document
                {
                    Id = Guid.NewGuid(),
                    OwnerUserId = studentId,
                    FileName = ar ? d.Ar : d.En,
                    ContentType = d.En.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg" : "application/pdf",
                    SizeBytes = rng.Next(80_000, 3_500_000),
                    StoragePath = $"documents/{Guid.NewGuid():N}",
                    Category = d.Cat,
                    UploadedAt = now.AddDays(-rng.Next(5, 360)),
                });
            }
        }
        for (var i = 0; i < docs.Count; i += 400)
        {
            db.Documents.AddRange(docs.GetRange(i, Math.Min(400, docs.Count - i)));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("Enriched document vault: {D} bilingual documents.", docs.Count);
    }

    private static async Task EnrichAuditLogAsync(
        ApplicationDbContext db, List<Guid> studentIds, Random rng, DateTimeOffset now,
        ILogger logger, CancellationToken ct)
    {
        if (await db.AuditLogs.CountAsync(ct).ConfigureAwait(false) > 200)
            return;

        var actions = new (AuditAction Action, string Target, string Summary)[]
        {
            (AuditAction.Login, "Session", "User signed in"),
            (AuditAction.LoginFailed, "Session", "Failed sign-in attempt"),
            (AuditAction.RoleChanged, "User", "Role granted after admin approval"),
            (AuditAction.Approved, "Onboarding", "Onboarding request approved"),
            (AuditAction.Rejected, "Onboarding", "Onboarding request rejected"),
            (AuditAction.Moderated, "ForumPost", "Community content moderated"),
            (AuditAction.PasswordReset, "User", "Password reset completed"),
            (AuditAction.Update, "PlatformSetting", "Platform setting changed"),
        };
        var ips = new[] { "196.221.44.12", "156.198.129.20", "41.33.10.7", "197.44.201.9", "102.45.6.88" };

        var logs = new List<AuditLog>();
        for (var i = 0; i < 400; i++)
        {
            var a = actions[rng.Next(actions.Length)];
            logs.Add(new AuditLog
            {
                ActorUserId = studentIds[rng.Next(studentIds.Count)],
                Action = a.Action,
                TargetType = a.Target,
                TargetId = Guid.NewGuid(),
                Summary = a.Summary,
                IpAddress = ips[rng.Next(ips.Length)],
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)",
                OccurredAt = now.AddDays(-rng.Next(0, 360)).AddMinutes(-rng.Next(0, 1440)),
            });
        }
        for (var i = 0; i < logs.Count; i += 400)
        {
            db.AuditLogs.AddRange(logs.GetRange(i, Math.Min(400, logs.Count - i)));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
        }
        logger.LogInformation("Enriched audit log: {N} entries across the year.", logs.Count);
    }

    private static async Task EnrichAdminQueuesAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        // Put GENUINELY low-rated consultants / providers into the admin
        // low-rating review queues. Only flag accounts actually below the 2.5
        // threshold — flagging high-rated ones "so the queue isn't empty" made the
        // queue lie (a 4.5-rated provider under a "below 2.5" heading) AND, because
        // this runs on every startup, kept flagging the next-lowest few each time,
        // accumulating dozens of false flags. If nobody is genuinely low, the queue
        // is legitimately empty.
        var now = DateTimeOffset.UtcNow;
        // NOTE: pass parameters as an explicit array so `ct` binds to the
        // CancellationToken overload rather than being treated as a SQL param.
        var flaggedConsultants = await db.Database.ExecuteSqlRawAsync(@"
SET QUOTED_IDENTIFIER ON;
UPDATE UserProfiles SET ConsultantLowRatingFlaggedAt = {0}
WHERE UserId IN (
    SELECT TOP 6 UserId FROM UserProfiles
    WHERE ConsultantAverageRating IS NOT NULL AND ConsultantReviewCount >= 3
      AND ConsultantAverageRating < 2.5
      AND ConsultantLowRatingFlaggedAt IS NULL
    ORDER BY ConsultantAverageRating ASC);",
            new object[] { now.AddDays(-14) }, ct)
            .ConfigureAwait(false);

        var flaggedProviders = await db.Database.ExecuteSqlRawAsync(@"
SET QUOTED_IDENTIFIER ON;
UPDATE UserProfiles SET ScholarshipProviderLowRatingFlaggedAt = {0}
WHERE UserId IN (
    SELECT TOP 4 UserId FROM UserProfiles
    WHERE ScholarshipProviderAverageRating IS NOT NULL
      AND ScholarshipProviderAverageRating < 2.5
      AND ScholarshipProviderLowRatingFlaggedAt IS NULL
    ORDER BY ScholarshipProviderAverageRating ASC);",
            new object[] { now.AddDays(-10) }, ct)
            .ConfigureAwait(false);

        logger.LogInformation(
            "Enriched admin queues: flagged {C} low-rated consultants, {P} low-rated providers.",
            flaggedConsultants, flaggedProviders);
    }
}
