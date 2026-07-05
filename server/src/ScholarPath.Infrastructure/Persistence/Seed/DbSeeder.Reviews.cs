using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a rich, bilingual body of ratings so the consultant marketplace, the
/// consultant detail pages, and the scholarship-provider cards all look
/// realistic at scale for demos and screenshots:
/// <list type="bullet">
///   <item>Every generated consultant gets a realistic spread of COMPLETED
///   bookings with real <see cref="ConsultantReview"/> rows — comments in a
///   natural mix of Arabic and English, ratings weighted high with a genuine
///   tail of 2–3 star reviews. Each consultant's stored
///   <c>ConsultantAverageRating</c>/<c>ConsultantReviewCount</c> snapshot is set
///   from those real reviews, so the marketplace figure matches the list.</item>
///   <item>Every scholarship provider gets a realistic
///   <c>ScholarshipProviderAverageRating</c>/<c>ReviewCount</c> snapshot so the
///   provider cards on scholarship listings are populated.</item>
/// </list>
/// Idempotent: it treats "more than the curated handful of consultant reviews
/// already exist" as "already seeded" and no-ops on re-run.
/// </summary>
public static partial class DbSeeder
{
    // ── Bilingual student-voice review pools ─────────────────────────────────
    // A natural mix so rating pages show reviews in both languages, the way a
    // real Arabic-and-English student community would leave them.
    private static readonly string[] ConsultantReviewsEn =
    [
        "Incredibly helpful session — my statement of purpose is far stronger now.",
        "Clear, honest feedback on my shortlist. Saved me from applying to the wrong programmes.",
        "Great advice on funding strategy. Would book again without hesitation.",
        "Walked me through the whole scholarship timeline step by step. Very reassuring.",
        "Sharpened my CV and helped me tell a coherent story. Highly recommend.",
        "Patient and knowledgeable. Answered every question about UK admissions.",
        "The interview practice made a huge difference — I felt calm and prepared.",
        "Practical, no-fluff guidance. I left with a concrete action plan.",
        "Helped me reframe my research proposal so it actually stood out.",
        "Responsive and encouraging throughout. My essays improved a lot.",
        "Good session overall, though I wish we'd had a bit more time on essays.",
        "Solid advice, but some of it I'd already found online myself.",
        "Really understood my field and pointed me to funding I'd never heard of.",
        "Kind, professional, and genuinely invested in my success.",
        "Turned a vague plan into a clear, competitive application.",
    ];

    private static readonly string[] ConsultantReviewsAr =
    [
        "جلسة مفيدة جدًا — خطاب الغرض بتاعي بقى أقوى بكتير دلوقتي.",
        "ملاحظات صريحة وواضحة على قائمة جامعاتي، وفّرت عليّ إني أقدّم في برامج غلط.",
        "نصايح ممتازة عن استراتيجية التمويل. هحجز معاه تاني من غير تردد.",
        "مشّاني في جدول التقديم خطوة بخطوة، وطمّنّي جدًا.",
        "ظبّط سيرتي الذاتية وساعدني أحكي قصتي بشكل مترابط. أنصح بيه بشدة.",
        "صبور وواسع المعرفة، ردّ على كل أسئلتي عن القبول في بريطانيا.",
        "التدريب على المقابلة عمل فرق كبير — حسّيت إني هادي ومستعد.",
        "إرشاد عملي ومن غير حشو، خرجت بخطة واضحة أنفّذها.",
        "ساعدني أعيد صياغة مقترح البحث بحيث يلفت النظر فعلًا.",
        "متجاوب ومشجّع طول الوقت، ومقالاتي اتحسّنت كتير.",
        "جلسة كويسة بشكل عام، بس كنت أتمنى وقت أكتر على المقالات.",
        "نصايح جيدة، بس بعضها كنت لاقيه بنفسي على النت.",
        "فهم تخصصي كويس ودلّني على تمويل عمري ما سمعت عنه.",
        "لطيف ومحترف ومهتم فعلًا بنجاحي.",
        "حوّل خطة غامضة لطلب واضح وتنافسي.",
    ];

    // Weighted, realistic rating: skewed high with a real tail so averages land
    // around ~4.3 and star-distribution bars actually have shape.
    private static int PickRating(Random rng)
    {
        var roll = rng.Next(100);
        return roll < 54 ? 5
             : roll < 80 ? 4
             : roll < 92 ? 3
             : roll < 98 ? 2
             : 1;
    }

    private static string PickBilingual(Random rng, string[] en, string[] ar)
        => rng.Next(2) == 0 ? ar[rng.Next(ar.Length)] : en[rng.Next(en.Length)];

    private static async Task SeedBulkReviewsAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        // Idempotency guard — the curated consultant seeder adds 2 reviews; once
        // this bulk pass has run there are far more, so re-runs no-op.
        if (await db.ConsultantReviews.CountAsync(ct).ConfigureAwait(false) > 10)
        {
            return;
        }

        // Deterministic RNG so re-seeding a wiped DB yields the same demo data.
        var rng = new Random(20260705);
        var now = DateTimeOffset.UtcNow;

        // Resolve consultants / providers / students by ROLE from the DB — the
        // DemoUsers lists only hold the hand-curated accounts, but the bulk
        // generator inserts hundreds more directly, and those are exactly the
        // ones that populate the marketplace, so they must get ratings too.
        async Task<List<Guid>> UserIdsInRoleAsync(string role) =>
            await (from ur in db.UserRoles
                   join r in db.Roles on ur.RoleId equals r.Id
                   where r.Name == role
                   select ur.UserId)
                .Distinct()
                .ToListAsync(ct)
                .ConfigureAwait(false);

        var consultantIds = await UserIdsInRoleAsync("Consultant").ConfigureAwait(false);
        var providerIds = await UserIdsInRoleAsync("ScholarshipProvider").ConfigureAwait(false);

        // Reviewer pool — a capped sample of students is plenty to attribute
        // reviews to without loading all 1,700.
        var students = await (from ur in db.UserRoles
                              join r in db.Roles on ur.RoleId equals r.Id
                              where r.Name == "Student"
                              select ur.UserId)
            .Distinct()
            .Take(800)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (consultantIds.Count == 0 || students.Count == 0)
        {
            return;
        }

        // Build the whole booking+review set in memory (lightweight POCOs), then
        // persist in BATCHES with the change-tracker cleared between them. On a
        // small hosting tier a single ~3,000-entity SaveChanges holds far too much
        // in the EF change tracker and can push the app into a memory-pressure
        // recycle mid-seed — batching keeps the working set flat.
        var pending = new List<(ConsultantBooking booking, ConsultantReview review)>();

        foreach (var consultantId in consultantIds)
        {
            // Review volume per consultant: most have a healthy history; ~15%
            // are "new" with 0–1 reviews so the marketplace isn't uniformly busy.
            var volumeRoll = rng.Next(100);
            var target = volumeRoll < 15 ? rng.Next(0, 2)
                       : volumeRoll < 45 ? rng.Next(2, 5)
                       : rng.Next(5, 12);

            var sessionPrice = new[] { 30m, 35m, 45m, 60m, 80m }[rng.Next(5)];

            for (var k = 0; k < target; k++)
            {
                var studentId = students[rng.Next(students.Count)];
                var daysAgo = rng.Next(7, 210);
                var dur = new[] { 30, 45, 60 }[rng.Next(3)];
                var start = now.AddDays(-daysAgo).Date.AddHours(15 + (k % 5));

                var booking = new ConsultantBooking
                {
                    Id = Guid.NewGuid(),
                    StudentId = studentId,
                    ConsultantId = consultantId,
                    ScheduledStartAt = start,
                    ScheduledEndAt = start.AddMinutes(dur),
                    DurationMinutes = dur,
                    PriceUsd = sessionPrice,
                    Status = BookingStatus.Completed,
                    RequestedAt = start.AddDays(-6),
                    ConfirmedAt = start.AddDays(-5),
                    CompletedAt = start.AddMinutes(dur),
                    StudentJoinedAt = start,
                    ConsultantJoinedAt = start,
                    CreatedAt = start.AddDays(-6),
                };

                var rating = PickRating(rng);
                var review = new ConsultantReview
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    StudentId = studentId,
                    ConsultantId = consultantId,
                    Rating = rating,
                    Comment = PickBilingual(rng, ConsultantReviewsEn, ConsultantReviewsAr),
                    CreatedAt = booking.CompletedAt!.Value.AddHours(rng.Next(1, 60)),
                };

                pending.Add((booking, review));
            }
        }

        // Persist in batches, clearing the tracker each time to cap memory.
        const int batchSize = 250;
        var totalReviews = 0;
        for (var i = 0; i < pending.Count; i += batchSize)
        {
            var slice = pending.GetRange(i, Math.Min(batchSize, pending.Count - i));
            db.Bookings.AddRange(slice.Select(x => x.booking));
            db.ConsultantReviews.AddRange(slice.Select(x => x.review));
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
            db.ChangeTracker.Clear();
            totalReviews += slice.Count;
        }

        // Stamp each consultant's displayed snapshot with a single SET-BASED
        // update computed directly from the persisted reviews — reliable
        // regardless of list size and allocation-free (no per-profile loading).
        // SET QUOTED_IDENTIFIER ON so the UPDATE works against the filtered-index
        // tables in the model regardless of the session default.
        var consultantsRated = 0;
        if (db.Database.IsRelational())
        {
            await db.Database.ExecuteSqlRawAsync(@"
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
UPDATE p
   SET p.ConsultantReviewCount   = x.cnt,
       p.ConsultantAverageRating = x.avg
FROM UserProfiles p
JOIN (SELECT ConsultantId,
             COUNT(*) AS cnt,
             CAST(ROUND(AVG(CAST(Rating AS decimal(5,2))), 2) AS decimal(3,2)) AS avg
      FROM ConsultantReviews
      WHERE IsDeleted = 0 AND IsHiddenByAdmin = 0
      GROUP BY ConsultantId) x
  ON p.UserId = x.ConsultantId;", ct).ConfigureAwait(false);

            consultantsRated = await db.UserProfiles
                .CountAsync(p => p.ConsultantAverageRating != null, ct)
                .ConfigureAwait(false);
        }

        // Providers: a realistic synthetic snapshot so the scholarship-listing
        // provider cards are populated. (Provider reviews are 1:1 with a finalised
        // application, so we don't fabricate applications here.) Only ~60 rows —
        // load, set, save in one cheap pass.
        var providerProfiles = await db.UserProfiles
            .Where(p => providerIds.Contains(p.UserId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        foreach (var pp in providerProfiles)
        {
            var count = rng.Next(100) < 20 ? rng.Next(0, 3) : rng.Next(4, 40);
            pp.ScholarshipProviderReviewCount = count;
            pp.ScholarshipProviderAverageRating = count == 0
                ? null
                : Math.Round(3.6m + (decimal)rng.NextDouble() * 1.4m, 2, MidpointRounding.AwayFromZero);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        db.ChangeTracker.Clear();

        logger.LogInformation(
            "Seeded bulk ratings: {R} bilingual consultant reviews (batched) across {C} consultants " +
            "({Rated} rated snapshots); stamped {P} provider snapshots.",
            totalReviews, consultantIds.Count, consultantsRated, providerProfiles.Count);
    }
}
