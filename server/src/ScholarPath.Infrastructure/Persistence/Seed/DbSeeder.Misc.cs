using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the document vault: <see cref="Document"/> metadata rows (no real
    /// bytes — the <c>StoragePath</c> points at a demo blob URL) across a
    /// variety of <see cref="DocumentCategory"/> folders, owned by demo
    /// students, with a couple linked to a seeded application. Idempotent on
    /// <see cref="Document"/> being empty.
    /// </summary>
    private static async Task SeedDocumentsAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<ApplicationTracker> applications,
        ILogger logger, CancellationToken ct)
    {
        if (await db.Documents.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var s0 = users.Students[0].Id;
        var s1 = users.Students[1].Id;
        var s2 = users.Students[2].Id;

        // Link a couple of documents to an in-app application owned by the student.
        var s0App = applications.FirstOrDefault(a => a.StudentId == s0);

        Document Doc(Guid owner, string name, string contentType, long size, DocumentCategory cat, Guid? appId = null) => new()
        {
            OwnerUserId = owner,
            FileName = name,
            ContentType = contentType,
            SizeBytes = size,
            StoragePath = $"https://demo.blob/documents/{Guid.NewGuid():N}/{name}",
            Category = cat,
            UploadedAt = now.AddDays(-Random.Shared.Next(5, 60)),
            ApplicationTrackerId = appId,
            CreatedAt = now.AddDays(-Random.Shared.Next(5, 60)),
        };

        var documents = new List<Document>
        {
            Doc(s0, "academic-transcript.pdf", "application/pdf", 248_300, DocumentCategory.Transcript, s0App?.Id),
            Doc(s0, "personal-statement.pdf", "application/pdf", 96_500, DocumentCategory.PersonalStatement, s0App?.Id),
            Doc(s0, "recommendation-letter.pdf", "application/pdf", 132_000, DocumentCategory.RecommendationLetter),
            Doc(s0, "passport-scan.jpg", "image/jpeg", 412_000, DocumentCategory.IdentityDocument),
            Doc(s1, "degree-certificate.pdf", "application/pdf", 187_400, DocumentCategory.Certificate),
            Doc(s1, "ielts-result.pdf", "application/pdf", 78_900, DocumentCategory.ProofOfEnglish),
            Doc(s1, "resume.pdf", "application/pdf", 64_200, DocumentCategory.Resume),
            Doc(s2, "bank-statement.pdf", "application/pdf", 210_000, DocumentCategory.FinancialDocument),
            Doc(s2, "design-portfolio.pdf", "application/pdf", 1_540_000, DocumentCategory.Portfolio),
            Doc(s2, "misc-notes.txt", "text/plain", 4_096, DocumentCategory.Other),
        };

        db.Documents.AddRange(documents);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} document-vault rows across varied categories", documents.Count);
    }

    /// <summary>
    /// Seeds <see cref="Notification"/>s of a varied <see cref="NotificationType"/>
    /// (application, booking, payment, community, onboarding, broadcast) — a mix
    /// of read and unread, across channels and priorities — plus a baseline set
    /// of <see cref="NotificationPreference"/> rows for the primary demo users.
    /// Idempotent on <see cref="Notification"/> being empty.
    /// </summary>
    private static async Task SeedNotificationsAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.Notifications.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var idem = 0;

        Notification N(Guid recipient, NotificationType type, NotificationChannel channel,
            string titleEn, string titleAr, string bodyEn, string bodyAr,
            bool isRead, int priority, int ageDays, string? deepLink = null) => new()
            {
                RecipientUserId = recipient,
                Type = type,
                Channel = channel,
                TitleEn = titleEn,
                TitleAr = titleAr,
                BodyEn = bodyEn,
                BodyAr = bodyAr,
                DeepLink = deepLink,
                IsRead = isRead,
                ReadAt = isRead ? now.AddDays(-ageDays).AddHours(3) : null,
                Priority = priority,
                IdempotencyKey = $"seed-notif-{++idem:D4}",
                DispatchedAt = now.AddDays(-ageDays),
                DispatchSucceeded = true,
                CreatedAt = now.AddDays(-ageDays),
            };

        var s0 = users.Students[0].Id;
        var s1 = users.Students[1].Id;
        var s2 = users.Students[2].Id;
        var company = users.Companies[0].Id;
        var consultant = users.Consultants[0].Id;

        var notifications = new List<Notification>
        {
            // Application notifications
            N(s0, NotificationType.ApplicationSubmitted, NotificationChannel.InApp,
                "Application submitted", "تم تقديم الطلب",
                "Your application has been submitted successfully.", "تم تقديم طلبك بنجاح.",
                isRead: true, priority: 1, ageDays: 7, deepLink: "/student/applications"),
            N(s2, NotificationType.ApplicationStatusChanged, NotificationChannel.InApp,
                "Application accepted", "تم قبول الطلب",
                "Congratulations! Your application has been accepted.", "تهانينا! تم قبول طلبك.",
                isRead: false, priority: 2, ageDays: 20, deepLink: "/student/applications"),
            N(s0, NotificationType.ApplicationDeadlineApproaching, NotificationChannel.Email,
                "Deadline approaching", "اقتراب الموعد النهائي",
                "A scholarship you saved closes in three days.", "تنتهي منحة قمت بحفظها خلال ثلاثة أيام.",
                isRead: false, priority: 2, ageDays: 1, deepLink: "/student/scholarships"),
            N(s1, NotificationType.ApplicationDraftReminder, NotificationChannel.InApp,
                "You have a draft application", "لديك طلب كمسودة",
                "Finish and submit your draft application before the deadline.", "أكمل وقدّم طلبك المسودة قبل الموعد النهائي.",
                isRead: false, priority: 1, ageDays: 2, deepLink: "/student/applications"),

            // Booking notifications
            N(consultant, NotificationType.BookingRequested, NotificationChannel.InApp,
                "New booking request", "طلب حجز جديد",
                "A student requested a consultation session.", "طلب أحد الطلاب جلسة استشارية.",
                isRead: true, priority: 2, ageDays: 1, deepLink: "/consultant/bookings"),
            N(s1, NotificationType.BookingConfirmed, NotificationChannel.InApp,
                "Booking confirmed", "تم تأكيد الحجز",
                "Your consultation session has been confirmed.", "تم تأكيد جلستك الاستشارية.",
                isRead: true, priority: 1, ageDays: 1, deepLink: "/student/bookings"),
            N(s1, NotificationType.BookingReminder, NotificationChannel.Push,
                "Session reminder", "تذكير بالجلسة",
                "Your consultation session starts in one hour.", "تبدأ جلستك الاستشارية خلال ساعة.",
                isRead: false, priority: 3, ageDays: 0, deepLink: "/student/bookings"),
            N(s2, NotificationType.ConsultantRatingReceived, NotificationChannel.InApp,
                "Please rate your session", "يرجى تقييم جلستك",
                "How was your recent consultation? Leave a review.", "كيف كانت استشارتك الأخيرة؟ اترك تقييماً.",
                isRead: false, priority: 1, ageDays: 14, deepLink: "/student/bookings"),

            // Payment notifications
            N(s1, NotificationType.PaymentSuccess, NotificationChannel.Email,
                "Payment successful", "تمت عملية الدفع بنجاح",
                "Your payment for the consultation session was successful.", "تمت عملية الدفع لجلستك الاستشارية بنجاح.",
                isRead: true, priority: 1, ageDays: 12),
            N(consultant, NotificationType.PayoutCompleted, NotificationChannel.InApp,
                "Payout completed", "تم تحويل المستحقات",
                "Your payout has been transferred to your bank account.", "تم تحويل مستحقاتك إلى حسابك البنكي.",
                isRead: false, priority: 1, ageDays: 7, deepLink: "/consultant/earnings"),

            // Community notification
            N(s0, NotificationType.ReplyOnYourPost, NotificationChannel.InApp,
                "New reply on your post", "رد جديد على منشورك",
                "Someone replied to your forum question.", "قام أحدهم بالرد على سؤالك في المنتدى.",
                isRead: true, priority: 1, ageDays: 13, deepLink: "/student/community"),

            // Onboarding / admin notification
            N(s1, NotificationType.UpgradeRequestApproved, NotificationChannel.InApp,
                "Upgrade request approved", "تمت الموافقة على طلب الترقية",
                "Your request to become a Company has been approved.", "تمت الموافقة على طلبك لتصبح شركة.",
                isRead: true, priority: 2, ageDays: 20, deepLink: "/profile"),

            // Resource notification
            N(users.Consultants[2].Id, NotificationType.ResourceApproved, NotificationChannel.InApp,
                "Resource approved", "تمت الموافقة على المورد",
                "Your submitted resource has been published.", "تم نشر المورد الذي قدمته.",
                isRead: false, priority: 1, ageDays: 9, deepLink: "/student/resources"),

            // Broadcast
            N(company, NotificationType.Broadcast, NotificationChannel.InApp,
                "Platform update", "تحديث المنصة",
                "ScholarPath has launched new analytics dashboards for companies.", "أطلقت سكولرباث لوحات تحليلات جديدة للشركات.",
                isRead: false, priority: 1, ageDays: 5),
        };

        db.Notifications.AddRange(notifications);

        // --- notification preferences for the primary demo users --------
        var prefUsers = new[] { users.PrimaryStudent.Id, users.PrimaryCompany.Id, users.PrimaryConsultant.Id };
        var prefTypes = new[]
        {
            NotificationType.ApplicationStatusChanged,
            NotificationType.BookingConfirmed,
            NotificationType.PaymentSuccess,
        };
        var preferences = new List<NotificationPreference>();
        foreach (var userId in prefUsers)
        {
            foreach (var type in prefTypes)
            {
                // InApp always on; Email on for status changes only — a realistic mix.
                preferences.Add(new NotificationPreference
                {
                    UserId = userId,
                    Type = type,
                    Channel = NotificationChannel.InApp,
                    IsEnabled = true,
                    CreatedAt = now.AddDays(-80),
                });
                preferences.Add(new NotificationPreference
                {
                    UserId = userId,
                    Type = type,
                    Channel = NotificationChannel.Email,
                    IsEnabled = type == NotificationType.ApplicationStatusChanged,
                    CreatedAt = now.AddDays(-80),
                });
            }
        }

        db.NotificationPreferences.AddRange(preferences.DistinctBy(p => (p.UserId, p.Type, p.Channel)));
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Seeded {N} notifications (varied types, read/unread) and {P} notification preferences",
            notifications.Count, preferences.Count);
    }

    /// <summary>
    /// Seeds <see cref="SuccessStory"/> entries — a mix of approved/featured,
    /// approved/non-featured, an admin-curated anonymous story (no
    /// <c>StudentId</c>) and a not-yet-approved one. Idempotent on
    /// <see cref="SuccessStory"/> being empty.
    /// </summary>
    private static async Task SeedSuccessStoriesAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.SuccessStories.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var stories = new List<SuccessStory>
        {
            new()
            {
                StudentId = users.Students[2].Id,
                AuthorDisplayName = "Lina Haddad",
                HeadlineEn = "From Amman to a fully funded Master's in Toronto",
                HeadlineAr = "من عمّان إلى ماجستير ممول بالكامل في تورنتو",
                BodyEn = "ScholarPath helped me find the STEM Excellence Award and a consultant who reshaped my statement of purpose. I'm now studying in Canada with full funding.",
                BodyAr = "ساعدتني سكولرباث في العثور على جائزة التميز في العلوم وعلى مستشار أعاد صياغة خطاب الغرض الخاص بي. أدرس الآن في كندا بتمويل كامل.",
                ScholarshipNameEn = "Global Scholars STEM Excellence Award",
                ScholarshipNameAr = "جائزة التميز في العلوم والتكنولوجيا",
                CountryCode = "CA",
                IsApproved = true,
                IsFeatured = true,
                FeaturedOrder = 1,
                CreatedAt = now.AddDays(-30),
            },
            new()
            {
                StudentId = users.Students[1].Id,
                AuthorDisplayName = "Omar Khalil",
                HeadlineEn = "A partial grant that made my first year possible",
                HeadlineAr = "منحة جزئية جعلت سنتي الأولى ممكنة",
                BodyEn = "The FutureFund Bridge Grant covered the gap I could not afford. The tracker kept me on top of every deadline.",
                BodyAr = "غطت منحة جسر فيوتشر فند الفجوة التي لم أستطع تحملها. وأبقاني المتتبع على اطلاع بكل موعد نهائي.",
                ScholarshipNameEn = "FutureFund Undergraduate Bridge Grant",
                ScholarshipNameAr = "منحة جسر المرحلة الجامعية من فيوتشر فند",
                CountryCode = "US",
                IsApproved = true,
                IsFeatured = true,
                FeaturedOrder = 2,
                CreatedAt = now.AddDays(-22),
            },
            new()
            {
                // Admin-curated anonymous story — no StudentId.
                StudentId = null,
                AuthorDisplayName = "A ScholarPath graduate",
                HeadlineEn = "Mentorship turned a rejection into an offer",
                HeadlineAr = "حوّل الإرشاد رفضاً إلى قبول",
                BodyEn = "After one rejection I booked a consultant on ScholarPath. Their feedback transformed my next application, which was accepted.",
                BodyAr = "بعد رفض واحد حجزت مستشاراً على سكولرباث. غيّرت ملاحظاتهم طلبي التالي الذي تم قبوله.",
                CountryCode = "GB",
                IsApproved = true,
                IsFeatured = false,
                CreatedAt = now.AddDays(-15),
            },
            new()
            {
                // Not yet approved — sits in the admin moderation queue.
                StudentId = users.Students[3].Id,
                AuthorDisplayName = "Youssef Nabil",
                HeadlineEn = "My scholarship journey (pending review)",
                HeadlineAr = "رحلتي مع المنح (قيد المراجعة)",
                BodyEn = "Sharing my experience applying through ScholarPath. This story is awaiting admin approval.",
                BodyAr = "أشارك تجربتي في التقديم عبر سكولرباث. هذه القصة بانتظار موافقة الإدارة.",
                CountryCode = "EG",
                IsApproved = false,
                IsFeatured = false,
                CreatedAt = now.AddDays(-3),
            },
        };

        db.SuccessStories.AddRange(stories);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} success stories (approved/featured/anonymous/pending)", stories.Count);
    }

    /// <summary>
    /// Seeds AI usage telemetry: <see cref="AiInteraction"/> rows across every
    /// <see cref="AiFeature"/> (Recommendation / Eligibility / Chatbot), plus
    /// <see cref="AiRedactionAuditSample"/> rows — one reviewed (with a verdict)
    /// and one still pending — and a couple of <see cref="EducationEntry"/> rows
    /// on the student profiles. Also seeds curated <see cref="KnowledgeDocument"/>
    /// FAQ entries so the RAG chatbot has grounding content out of the box.
    /// Idempotent on <see cref="AiInteraction"/> / <see cref="EducationEntry"/> /
    /// <see cref="KnowledgeDocument"/> being empty respectively.
    /// </summary>
    private static async Task SeedAiAsync(
        ApplicationDbContext db, DemoUsers users, IReadOnlyList<Scholarship> scholarships,
        ILogger logger, CancellationToken ct)
    {
        await SeedEducationEntriesAsync(db, users, logger, ct).ConfigureAwait(false);
        await SeedKnowledgeDocumentsAsync(db, logger, ct).ConfigureAwait(false);

        if (await db.AiInteractions.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var recommendation = new AiInteraction
        {
            UserId = users.Students[0].Id,
            Feature = AiFeature.Recommendation,
            Provider = AiProvider.Stub,
            ModelName = "scholarpath-reco-stub-v1",
            SessionId = $"sess-{Guid.NewGuid():N}",
            PromptText = "Recommend scholarships for a Computer Science undergraduate with a 3.8 GPA.",
            ResponseText = "Top matches: Global Scholars STEM Excellence Award, Young Entrepreneurs Tuition Scholarship.",
            PromptTokens = 42,
            CompletionTokens = 64,
            CostUsd = 0.0021m,
            MetadataJson = """{"scores":[0.92,0.81]}""",
            StartedAt = now.AddDays(-6),
            CompletedAt = now.AddDays(-6).AddSeconds(3),
            CreatedAt = now.AddDays(-6),
        };
        var eligibility = new AiInteraction
        {
            UserId = users.Students[1].Id,
            Feature = AiFeature.Eligibility,
            Provider = AiProvider.Stub,
            ModelName = "scholarpath-eligibility-stub-v1",
            SessionId = $"sess-{Guid.NewGuid():N}",
            PromptText = "Am I eligible for the Nile Bridge Doctoral Research Fellowship?",
            ResponseText = "You appear eligible: you hold a relevant Master's degree. A research proposal is still required.",
            PromptTokens = 38,
            CompletionTokens = 51,
            CostUsd = 0.0018m,
            MetadataJson = """{"verdict":"likely-eligible","missing":["research-proposal"]}""",
            StartedAt = now.AddDays(-4),
            CompletedAt = now.AddDays(-4).AddSeconds(2),
            CreatedAt = now.AddDays(-4),
        };
        var chatbot = new AiInteraction
        {
            UserId = users.Students[2].Id,
            Feature = AiFeature.Chatbot,
            Provider = AiProvider.Stub,
            ModelName = "scholarpath-chat-stub-v1",
            SessionId = $"sess-{Guid.NewGuid():N}",
            PromptText = "How do I withdraw an application? My email is redacted@example.com.",
            ResponseText = "Open the application from your tracker and choose \"Withdraw\". This cannot be undone.",
            PromptTokens = 29,
            CompletionTokens = 33,
            CostUsd = 0.0012m,
            StartedAt = now.AddDays(-2),
            CompletedAt = now.AddDays(-2).AddSeconds(2),
            CreatedAt = now.AddDays(-2),
        };

        var interactions = new[] { recommendation, eligibility, chatbot };
        db.AiInteractions.AddRange(interactions);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // Recommendation click event — the student clicked through to a scholarship.
        var firstScholarship = scholarships.FirstOrDefault(s => s.Status == ScholarshipStatus.Open);
        if (firstScholarship is not null)
        {
            db.RecommendationClickEvents.Add(new RecommendationClickEvent
            {
                UserId = users.Students[0].Id,
                ScholarshipId = firstScholarship.Id,
                AiInteractionId = recommendation.Id,
                ClickedAt = now.AddDays(-6).AddMinutes(2),
                Source = "card",
            });
        }

        // Redaction audit samples — one reviewed, one pending (unique AiInteractionId).
        db.AiRedactionAuditSamples.AddRange(
            new AiRedactionAuditSample
            {
                AiInteractionId = chatbot.Id,
                UserId = chatbot.UserId,
                RedactedPrompt = "How do I withdraw an application? My email is [REDACTED_EMAIL].",
                SampledAt = now.AddDays(-1),
                Verdict = RedactionVerdict.Clean,
                ReviewerUserId = users.PrimaryAdmin.Id,
                ReviewedAt = now.AddHours(-12),
            },
            new AiRedactionAuditSample
            {
                AiInteractionId = eligibility.Id,
                UserId = eligibility.UserId,
                RedactedPrompt = "Am I eligible for the Nile Bridge Doctoral Research Fellowship?",
                SampledAt = now.AddHours(-6),
                Verdict = null, // not yet reviewed
            });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Seeded {N} AI interactions (all features), 1 recommendation click, 2 redaction audit samples",
            interactions.Length);
    }

    /// <summary>
    /// Seeds <see cref="EducationEntry"/> rows against the demo student profiles.
    /// Idempotent on the table being empty.
    /// </summary>
    private static async Task SeedEducationEntriesAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        if (await db.EducationEntries.AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var studentIds = users.Students.Select(s => s.Id).ToList();
        var profiles = await db.UserProfiles
            .Where(p => studentIds.Contains(p.UserId))
            .ToListAsync(ct).ConfigureAwait(false);
        if (profiles.Count == 0)
        {
            return;
        }

        var entries = new List<EducationEntry>();
        foreach (var profile in profiles.Take(3))
        {
            entries.Add(new EducationEntry
            {
                UserProfileId = profile.Id,
                InstitutionName = profile.CurrentInstitution ?? "Cairo University",
                Degree = "Bachelor of Science",
                FieldOfStudy = profile.FieldOfStudy ?? "Computer Science",
                StartDate = new DateOnly(2020, 9, 1),
                EndDate = new DateOnly(2024, 6, 30),
                Gpa = profile.Gpa ?? 3.6m,
                Description = "Completed an undergraduate degree with a focus on the chosen field of study.",
                CreatedAt = DateTimeOffset.UtcNow.AddDays(-70),
            });
        }

        db.EducationEntries.AddRange(entries);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation("Seeded {N} education entries", entries.Count);
    }

    /// <summary>
    /// Seeds curated FAQ-style <see cref="KnowledgeDocument"/>s that ground
    /// the RAG chatbot on the platform's most common questions — terminology,
    /// the application process, recommendation letters, language tests,
    /// visas, and country-specific guidance. Idempotent per
    /// (<see cref="KnowledgeSourceType"/>, <c>SourceKey</c>): rows already
    /// present are left untouched so the documents are safe to extend on
    /// later seed runs.
    ///
    /// Embeddings are NOT computed here. The rows are inserted with empty
    /// <c>Embedding</c> and the next call to the knowledge-base indexer
    /// (manual rebuild or scheduled job) will fill them in. This keeps the
    /// seeder fast and free of any dependency on the embedding provider.
    /// </summary>
    private static async Task SeedKnowledgeDocumentsAsync(
        ApplicationDbContext db, ILogger logger, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        // Each entry is a (SourceKey, TitleEn, TitleAr, ContentEn, ContentAr)
        // tuple. SourceKey is the stable upsert key — the indexer reuses it
        // when re-running.
        var faqs = new (string Key, string TitleEn, string TitleAr, string TagsCsv, string ContentEn, string ContentAr)[]
        {
            (
                "platform-faq",
                "ScholarPath Platform FAQ",
                "الأسئلة الشائعة عن منصة سكولرباث",
                "platform,faq,onboarding",
                KnowledgeBodies.PlatformFaqEn,
                KnowledgeBodies.PlatformFaqAr
            ),
            (
                "scholarship-terminology",
                "Scholarship Terminology — Fully Funded vs Partial",
                "مصطلحات المنح — التمويل الكامل والجزئي",
                "terminology,funding",
                KnowledgeBodies.TerminologyEn,
                KnowledgeBodies.TerminologyAr
            ),
            (
                "sop-writing-guide",
                "Writing a Strong Statement of Purpose",
                "كتابة خطاب غرض قوي",
                "essays,sop,writing",
                KnowledgeBodies.SopWritingEn,
                KnowledgeBodies.SopWritingAr
            ),
            (
                "recommendation-letters",
                "Recommendation Letter Best Practices",
                "أفضل ممارسات خطابات التوصية",
                "recommendations,applications",
                KnowledgeBodies.RecommendationFaqEn,
                KnowledgeBodies.RecommendationFaqAr
            ),
            (
                "visa-uk",
                "UK Student Visa — Requirements Summary",
                "تأشيرة الطالب البريطانية — ملخص المتطلبات",
                "visa,uk",
                KnowledgeBodies.VisaUkEn,
                KnowledgeBodies.VisaUkAr
            ),
            (
                "visa-us",
                "US Student Visa — F-1 Requirements Summary",
                "تأشيرة الطالب الأمريكية — ملخص متطلبات F-1",
                "visa,us",
                KnowledgeBodies.VisaUsEn,
                KnowledgeBodies.VisaUsAr
            ),
            (
                "visa-schengen",
                "Schengen Student Visa — Requirements Summary",
                "تأشيرة الطالب في منطقة شنغن — ملخص المتطلبات",
                "visa,schengen,europe",
                KnowledgeBodies.VisaSchengenEn,
                KnowledgeBodies.VisaSchengenAr
            ),
            (
                "ielts-prep",
                "IELTS Preparation — What Score You Need and How To Get It",
                "التحضير للآيلتس — الدرجة المطلوبة وكيفية الحصول عليها",
                "language,ielts",
                KnowledgeBodies.IeltsPrepEn,
                KnowledgeBodies.IeltsPrepAr
            ),
            (
                "toefl-prep",
                "TOEFL Preparation — Format, Scores and a Study Plan",
                "التحضير للتوفل — الصيغة والدرجات وخطة الدراسة",
                "language,toefl",
                KnowledgeBodies.ToeflPrepEn,
                KnowledgeBodies.ToeflPrepAr
            ),
            (
                "country-guide-germany",
                "Country Guide — Studying in Germany",
                "دليل الدولة — الدراسة في ألمانيا",
                "country,germany,europe",
                KnowledgeBodies.CountryGermanyEn,
                KnowledgeBodies.CountryGermanyAr
            ),
            (
                "country-guide-canada",
                "Country Guide — Studying in Canada",
                "دليل الدولة — الدراسة في كندا",
                "country,canada,north-america",
                KnowledgeBodies.CountryCanadaEn,
                KnowledgeBodies.CountryCanadaAr
            ),
            (
                "country-guide-uk",
                "Country Guide — Studying in the United Kingdom",
                "دليل الدولة — الدراسة في المملكة المتحدة",
                "country,uk,europe",
                KnowledgeBodies.CountryUkEn,
                KnowledgeBodies.CountryUkAr
            ),
            (
                "country-guide-usa",
                "Country Guide — Studying in the United States",
                "دليل الدولة — الدراسة في الولايات المتحدة",
                "country,usa,north-america",
                KnowledgeBodies.CountryUsaEn,
                KnowledgeBodies.CountryUsaAr
            ),
        };

        // Idempotent per (SourceType, SourceKey): only add missing rows.
        var existingKeys = await db.KnowledgeDocuments
            .Where(d => d.SourceType == KnowledgeSourceType.Faq)
            .Select(d => d.SourceKey)
            .ToListAsync(ct).ConfigureAwait(false);

        var existingSet = new HashSet<string>(existingKeys, StringComparer.Ordinal);
        var toAdd = new List<KnowledgeDocument>();

        foreach (var f in faqs)
        {
            if (existingSet.Contains(f.Key)) continue;

            // The indexer's content shape: a "Question/Answer" prefix per
            // language. We keep the same shape so an indexer rebuild can
            // pick these up and re-key cleanly.
            var contentEn = $"Question: {f.TitleEn}\nAnswer: {f.ContentEn}";
            var contentAr = $"سؤال: {f.TitleAr}\nالإجابة: {f.ContentAr}";
            var hash = ComputeContentHash(contentEn + "\n\n" + contentAr);

            toAdd.Add(new KnowledgeDocument
            {
                SourceType = KnowledgeSourceType.Faq,
                SourceKey = f.Key,
                SourceId = null,
                TitleEn = f.TitleEn,
                TitleAr = f.TitleAr,
                ContentEn = contentEn,
                ContentAr = contentAr,
                ContentHash = hash,
                MetadataJson = $$"""{"tags":["{{string.Join("\",\"", f.TagsCsv.Split(','))}}"]}""",
                Embedding = [],
                EmbeddingDimensions = 0,
                EmbeddingModel = null,
                CreatedAt = now,
            });
        }

        if (toAdd.Count == 0)
        {
            return;
        }

        db.KnowledgeDocuments.AddRange(toAdd);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        logger.LogInformation(
            "Seeded {N} curated FAQ knowledge documents (pending embedding)",
            toAdd.Count);
    }

    /// <summary>
    /// SHA-256 of the text the embedder consumes — matches the hash the
    /// knowledge-base indexer computes so a later rebuild correctly detects
    /// unchanged content and skips re-embedding it.
    /// </summary>
    private static string ComputeContentHash(string text)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
