using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the whole consultant module:
    /// <list type="bullet">
    ///   <item><see cref="ConsultantAvailability"/> — recurring weekly slots
    ///   plus a couple of ad-hoc one-off slots, for the active consultants.</item>
    ///   <item><see cref="ConsultantBooking"/> — bookings covering EVERY
    ///   <see cref="BookingStatus"/> (Requested / Confirmed / Rejected / Expired /
    ///   Cancelled / Completed / NoShowStudent / NoShowConsultant).</item>
    ///   <item><see cref="ConsultantReview"/> — reviews on the completed
    ///   bookings.</item>
    /// </list>
    /// The DB enforces a unique filtered index <c>UX_Bookings_Consultant_Slot_Active</c>
    /// over <c>(ConsultantId, ScheduledStartAt)</c> for the Requested/Confirmed
    /// statuses, so every live booking here is given a distinct start time.
    /// Returns the seeded bookings so the payment seeder can link to them.
    /// </summary>
    private static async Task<IReadOnlyList<ConsultantBooking>> SeedConsultantModuleAsync(
        ApplicationDbContext db, DemoUsers users, ILogger logger, CancellationToken ct)
    {
        var existing = await db.Bookings.IgnoreQueryFilters().ToListAsync(ct).ConfigureAwait(false);
        if (existing.Count > 0)
        {
            return existing;
        }

        var now = DateTimeOffset.UtcNow;

        // The bookable consultants are the verified, active ones.
        var c1 = users.Consultants[0]; // Hana — primary
        var c2 = users.Consultants[1]; // Tarek
        var c3 = users.Consultants[2]; // Nour
        var c4 = users.Consultants[3]; // James

        // ---------------------------------------------------------------
        // Availability — recurring weekly + ad-hoc.
        // ---------------------------------------------------------------
        var availabilities = new List<ConsultantAvailability>();
        var weeklyConsultants = new[] { c1, c2, c3, c4 };
        var weekdays = new[] { DayOfWeek.Sunday, DayOfWeek.Tuesday, DayOfWeek.Thursday };
        foreach (var c in weeklyConsultants)
        {
            foreach (var day in weekdays)
            {
                availabilities.Add(new ConsultantAvailability
                {
                    ConsultantId = c.Id,
                    DayOfWeek = day,
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(20, 0),
                    Timezone = "Africa/Cairo",
                    IsRecurring = true,
                    IsActive = true,
                    CreatedAt = now.AddDays(-60),
                });
            }
        }

        // Ad-hoc one-off slots for the next couple of weeks.
        availabilities.Add(new ConsultantAvailability
        {
            ConsultantId = c1.Id,
            SpecificStartAt = now.AddDays(3).Date.AddHours(13),
            SpecificEndAt = now.AddDays(3).Date.AddHours(15),
            Timezone = "Africa/Cairo",
            IsRecurring = false,
            IsActive = true,
            CreatedAt = now.AddDays(-2),
        });
        availabilities.Add(new ConsultantAvailability
        {
            ConsultantId = c2.Id,
            SpecificStartAt = now.AddDays(6).Date.AddHours(10),
            SpecificEndAt = now.AddDays(6).Date.AddHours(12),
            Timezone = "Asia/Dubai",
            IsRecurring = false,
            IsActive = true,
            CreatedAt = now.AddDays(-1),
        });
        // An inactive (withdrawn) availability — exercises the IsActive filter.
        availabilities.Add(new ConsultantAvailability
        {
            ConsultantId = c3.Id,
            DayOfWeek = DayOfWeek.Friday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(11, 0),
            Timezone = "Asia/Amman",
            IsRecurring = true,
            IsActive = false,
            CreatedAt = now.AddDays(-50),
        });

        db.Availabilities.AddRange(availabilities);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ---------------------------------------------------------------
        // Bookings — one per BookingStatus. Distinct start times per consultant.
        // ---------------------------------------------------------------
        var s0 = users.Students[0];
        var s1 = users.Students[1];
        var s2 = users.Students[2];
        var s3 = users.Students[3];

        var bookings = new List<ConsultantBooking>
        {
            // Requested — pending consultant acceptance (future slot)
            new()
            {
                StudentId = s0.Id, ConsultantId = c1.Id,
                ScheduledStartAt = now.AddDays(4).Date.AddHours(16),
                ScheduledEndAt = now.AddDays(4).Date.AddHours(16).AddMinutes(45),
                DurationMinutes = 45, PriceUsd = 45m,
                Status = BookingStatus.Requested,
                RequestedAt = now.AddHours(-6),
                StripePaymentIntentId = "pi_demo_booking_requested",
                CreatedAt = now.AddHours(-6),
            },
            // Confirmed — accepted, upcoming (future slot, distinct from above)
            new()
            {
                StudentId = s1.Id, ConsultantId = c1.Id,
                ScheduledStartAt = now.AddDays(5).Date.AddHours(17),
                ScheduledEndAt = now.AddDays(5).Date.AddHours(17).AddMinutes(45),
                DurationMinutes = 45, PriceUsd = 45m,
                Status = BookingStatus.Confirmed,
                RequestedAt = now.AddDays(-2), ConfirmedAt = now.AddDays(-1),
                StripePaymentIntentId = "pi_demo_booking_confirmed",
                CreatedAt = now.AddDays(-2),
            },
            // Rejected — consultant declined
            new()
            {
                StudentId = s2.Id, ConsultantId = c2.Id,
                ScheduledStartAt = now.AddDays(-1).Date.AddHours(16),
                ScheduledEndAt = now.AddDays(-1).Date.AddHours(17),
                DurationMinutes = 60, PriceUsd = 60m,
                Status = BookingStatus.Rejected,
                RequestedAt = now.AddDays(-5), RejectedAt = now.AddDays(-4),
                CancellationReason = CancellationReason.RejectedByConsultant,
                CancelledByUserId = c2.Id,
                CreatedAt = now.AddDays(-5),
            },
            // Expired — no consultant response in time
            new()
            {
                StudentId = s3.Id, ConsultantId = c2.Id,
                ScheduledStartAt = now.AddDays(-2).Date.AddHours(18),
                ScheduledEndAt = now.AddDays(-2).Date.AddHours(19),
                DurationMinutes = 60, PriceUsd = 60m,
                Status = BookingStatus.Expired,
                RequestedAt = now.AddDays(-9), ExpiredAt = now.AddDays(-7),
                CancellationReason = CancellationReason.AutoExpiredNoResponse,
                CreatedAt = now.AddDays(-9),
            },
            // Cancelled — student cancelled before the session
            new()
            {
                StudentId = s0.Id, ConsultantId = c3.Id,
                ScheduledStartAt = now.AddDays(-3).Date.AddHours(16),
                ScheduledEndAt = now.AddDays(-3).Date.AddHours(16).AddMinutes(30),
                DurationMinutes = 30, PriceUsd = 35m,
                Status = BookingStatus.Cancelled,
                RequestedAt = now.AddDays(-10), ConfirmedAt = now.AddDays(-9),
                CancelledAt = now.AddDays(-6),
                CancellationReason = CancellationReason.StudentCancelledMoreThan24HoursBefore,
                CancelledByUserId = s0.Id,
                CreatedAt = now.AddDays(-10),
            },
            // Completed #1 — finished, will get a review
            new()
            {
                StudentId = s1.Id, ConsultantId = c1.Id,
                ScheduledStartAt = now.AddDays(-12).Date.AddHours(16),
                ScheduledEndAt = now.AddDays(-12).Date.AddHours(16).AddMinutes(45),
                DurationMinutes = 45, PriceUsd = 45m,
                Status = BookingStatus.Completed,
                RequestedAt = now.AddDays(-18), ConfirmedAt = now.AddDays(-17),
                CompletedAt = now.AddDays(-12),
                StripePaymentIntentId = "pi_demo_booking_completed_1",
                CreatedAt = now.AddDays(-18),
            },
            // Completed #2 — finished, will get a review
            new()
            {
                StudentId = s2.Id, ConsultantId = c4.Id,
                ScheduledStartAt = now.AddDays(-15).Date.AddHours(17),
                ScheduledEndAt = now.AddDays(-15).Date.AddHours(18),
                DurationMinutes = 60, PriceUsd = 80m,
                Status = BookingStatus.Completed,
                RequestedAt = now.AddDays(-22), ConfirmedAt = now.AddDays(-21),
                CompletedAt = now.AddDays(-15),
                StripePaymentIntentId = "pi_demo_booking_completed_2",
                CreatedAt = now.AddDays(-22),
            },
            // NoShowStudent — student did not attend
            new()
            {
                StudentId = s3.Id, ConsultantId = c1.Id,
                ScheduledStartAt = now.AddDays(-8).Date.AddHours(18),
                ScheduledEndAt = now.AddDays(-8).Date.AddHours(18).AddMinutes(45),
                DurationMinutes = 45, PriceUsd = 45m,
                Status = BookingStatus.NoShowStudent,
                RequestedAt = now.AddDays(-14), ConfirmedAt = now.AddDays(-13),
                IsNoShowStudent = true, NoShowMarkedAt = now.AddDays(-8),
                CancellationReason = CancellationReason.StudentNoShow,
                StripePaymentIntentId = "pi_demo_booking_noshow_student",
                CreatedAt = now.AddDays(-14),
            },
            // NoShowConsultant — consultant did not attend
            new()
            {
                StudentId = s0.Id, ConsultantId = c3.Id,
                ScheduledStartAt = now.AddDays(-6).Date.AddHours(16),
                ScheduledEndAt = now.AddDays(-6).Date.AddHours(16).AddMinutes(30),
                DurationMinutes = 30, PriceUsd = 35m,
                Status = BookingStatus.NoShowConsultant,
                RequestedAt = now.AddDays(-11), ConfirmedAt = now.AddDays(-10),
                IsNoShowConsultant = true, NoShowMarkedAt = now.AddDays(-6),
                CancellationReason = CancellationReason.ConsultantNoShow,
                StripePaymentIntentId = "pi_demo_booking_noshow_consultant",
                CreatedAt = now.AddDays(-11),
            },
        };

        db.Bookings.AddRange(bookings);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ---------------------------------------------------------------
        // Reviews — one per completed booking (unique BookingId index).
        // ---------------------------------------------------------------
        var completed = bookings.Where(b => b.Status == BookingStatus.Completed).ToList();
        var reviews = new List<ConsultantReview>();
        if (completed.Count > 0)
        {
            reviews.Add(new ConsultantReview
            {
                BookingId = completed[0].Id,
                StudentId = completed[0].StudentId,
                ConsultantId = completed[0].ConsultantId,
                Rating = 5,
                Comment = "Incredibly helpful session — my statement of purpose is far stronger now.",
                CreatedAt = completed[0].CompletedAt!.Value.AddHours(2),
            });
        }

        if (completed.Count > 1)
        {
            reviews.Add(new ConsultantReview
            {
                BookingId = completed[1].Id,
                StudentId = completed[1].StudentId,
                ConsultantId = completed[1].ConsultantId,
                Rating = 4,
                Comment = "Great advice on funding strategy. Would book again.",
                IsHiddenByAdmin = false,
                CreatedAt = completed[1].CompletedAt!.Value.AddHours(5),
            });
        }

        if (reviews.Count > 0)
        {
            db.ConsultantReviews.AddRange(reviews);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Seeded {A} availabilities, {B} bookings (all statuses), {R} consultant reviews",
            availabilities.Count, bookings.Count, reviews.Count);
        return bookings;
    }
}
