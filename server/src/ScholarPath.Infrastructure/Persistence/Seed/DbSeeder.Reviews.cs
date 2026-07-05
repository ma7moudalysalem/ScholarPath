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

        // Load the consultant + provider profiles once so we can stamp their
        // displayed rating snapshots.
        var allRatedIds = consultantIds.Concat(providerIds).ToList();
        var profiles = await db.UserProfiles
            .Where(p => allRatedIds.Contains(p.UserId))
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var profileByUser = profiles.ToDictionary(p => p.UserId);

        // Bookings that already exist (from the curated seeder) so we don't clash
        // and so those consultants' counts include the curated reviews.
        var existingReviewByConsultant = await db.ConsultantReviews
            .GroupBy(r => r.ConsultantId)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count(), Sum = g.Sum(r => r.Rating) })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        var priorByConsultant = existingReviewByConsultant
            .ToDictionary(x => x.ConsultantId, x => (count: x.Count, sum: x.Sum));

        var newBookings = new List<ConsultantBooking>();
        var newReviews = new List<ConsultantReview>();

        foreach (var consultantId in consultantIds)
        {
            // Review volume per consultant: most have a healthy history; ~15%
            // are "new" with 0–1 reviews so the marketplace isn't uniformly busy.
            var volumeRoll = rng.Next(100);
            var target = volumeRoll < 15 ? rng.Next(0, 2)
                       : volumeRoll < 45 ? rng.Next(2, 5)
                       : rng.Next(5, 12);

            var priorCount = priorByConsultant.TryGetValue(consultantId, out var prior) ? prior.count : 0;
            var priorSum = priorCount > 0 ? prior.sum : 0;

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
                newBookings.Add(booking);

                var rating = PickRating(rng);
                priorSum += rating;
                priorCount++;

                newReviews.Add(new ConsultantReview
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    StudentId = studentId,
                    ConsultantId = consultantId,
                    Rating = rating,
                    Comment = PickBilingual(rng, ConsultantReviewsEn, ConsultantReviewsAr),
                    CreatedAt = booking.CompletedAt!.Value.AddHours(rng.Next(1, 60)),
                });
            }

            // Stamp the displayed snapshot from the REAL reviews (curated + new).
            if (profileByUser.TryGetValue(consultantId, out var cp))
            {
                cp.ConsultantReviewCount = priorCount;
                cp.ConsultantAverageRating = priorCount == 0
                    ? null
                    : Math.Round((decimal)priorSum / priorCount, 2, MidpointRounding.AwayFromZero);
            }
        }

        // Providers: give every provider a realistic displayed snapshot so the
        // scholarship-listing provider cards are populated. (Provider reviews are
        // 1:1 with a finalised application, so we don't fabricate applications
        // here — the snapshot is what the cards read.)
        foreach (var providerId in providerIds)
        {
            if (!profileByUser.TryGetValue(providerId, out var pp)) continue;
            var count = rng.Next(100) < 20 ? rng.Next(0, 3) : rng.Next(4, 40);
            pp.ScholarshipProviderReviewCount = count;
            pp.ScholarshipProviderAverageRating = count == 0
                ? null
                : Math.Round(3.6m + (decimal)rng.NextDouble() * 1.4m, 2, MidpointRounding.AwayFromZero);
        }

        if (newBookings.Count > 0)
        {
            db.Bookings.AddRange(newBookings);
            db.ConsultantReviews.AddRange(newReviews);
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        logger.LogInformation(
            "Seeded bulk ratings: {B} completed bookings, {R} bilingual consultant reviews across {C} consultants; " +
            "stamped provider rating snapshots on {P} providers.",
            newBookings.Count, newReviews.Count, consultantIds.Count, providerIds.Count);
    }
}
