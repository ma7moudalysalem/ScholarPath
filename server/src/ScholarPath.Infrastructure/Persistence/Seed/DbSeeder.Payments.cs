using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Infrastructure.Persistence.Seed;

public static partial class DbSeeder
{
    /// <summary>
    /// Seeds the payments domain:
    /// <list type="bullet">
    ///   <item><see cref="Payment"/> rows covering EVERY <see cref="PaymentStatus"/>
    ///   (Pending / Held / Captured / Refunded / PartiallyRefunded / Failed /
    ///   Cancelled / Disputed) across BOTH <see cref="PaymentType"/>s
    ///   (ConsultantBooking, CompanyReview).</item>
    ///   <item><see cref="Payout"/> rows covering EVERY <see cref="PayoutStatus"/>
    ///   (Pending / InTransit / Paid / Failed).</item>
    ///   <item><see cref="CompanyReview"/> + <see cref="CompanyReviewPayment"/>
    ///   on finalised applications.</item>
    /// </list>
    /// Amounts are in cents. The 10% / 15% profit-share split is applied so the
    /// numbers reconcile. Idempotent on <see cref="Payment"/> being empty.
    /// </summary>
    private static async Task SeedPaymentsAsync(
        ApplicationDbContext db, DemoUsers users,
        IReadOnlyList<ApplicationTracker> applications, IReadOnlyList<ConsultantBooking> bookings,
        ILogger logger, CancellationToken ct)
    {
        if (await db.Payments.IgnoreQueryFilters().AnyAsync(ct).ConfigureAwait(false))
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var key = 0;
        string NextKey(string prefix) => $"{prefix}-{++key:D4}-{Guid.NewGuid():N}";

        // Helper that builds a consultant-booking payment with the 10% split.
        Payment BookingPayment(ConsultantBooking b, PaymentStatus status, Action<Payment>? extra = null)
        {
            var amount = (long)(b.PriceUsd * 100);
            var profit = amount / 10; // 10% consultant-booking profit share
            var p = new Payment
            {
                Type = PaymentType.ConsultantBooking,
                Status = status,
                AmountCents = amount,
                Currency = "USD",
                ProfitShareAmountCents = profit,
                PayeeAmountCents = amount - profit,
                PayerUserId = b.StudentId,
                PayeeUserId = b.ConsultantId,
                StripePaymentIntentId = b.StripePaymentIntentId ?? $"pi_demo_{Guid.NewGuid():N}",
                StripeChargeId = $"ch_demo_{Guid.NewGuid():N}",
                IdempotencyKey = NextKey("booking"),
                RelatedBookingId = b.Id,
                CreatedAt = b.CreatedAt,
            };
            extra?.Invoke(p);
            return p;
        }

        // Helper that builds a company-review payment with the 15% split.
        Payment ReviewPayment(ApplicationTracker app, Guid companyId, long amountCents, PaymentStatus status, Action<Payment>? extra = null)
        {
            var profit = amountCents * 15 / 100; // 15% company-review profit share
            var p = new Payment
            {
                Type = PaymentType.CompanyReview,
                Status = status,
                AmountCents = amountCents,
                Currency = "USD",
                ProfitShareAmountCents = profit,
                PayeeAmountCents = amountCents - profit,
                PayerUserId = companyId,
                PayeeUserId = companyId, // company is both payer and payee for review fees in the demo
                StripePaymentIntentId = $"pi_demo_review_{Guid.NewGuid():N}",
                StripeChargeId = $"ch_demo_review_{Guid.NewGuid():N}",
                IdempotencyKey = NextKey("review"),
                RelatedApplicationId = app.Id,
                CreatedAt = app.CreatedAt,
            };
            extra?.Invoke(p);
            return p;
        }

        var payments = new List<Payment>();

        // ---- consultant-booking payments, one per status ----------------
        // Map distinct bookings to statuses so RelatedBookingId is meaningful.
        var byStatus = bookings.GroupBy(b => b.Status).ToDictionary(g => g.Key, g => g.ToList());
        ConsultantBooking? Pick(BookingStatus s) => byStatus.TryGetValue(s, out var l) && l.Count > 0 ? l[0] : null;
        ConsultantBooking? PickSecond(BookingStatus s) => byStatus.TryGetValue(s, out var l) && l.Count > 1 ? l[1] : null;

        var completed1 = Pick(BookingStatus.Completed);
        var completed2 = PickSecond(BookingStatus.Completed);
        var confirmed = Pick(BookingStatus.Confirmed);
        var requested = Pick(BookingStatus.Requested);
        var cancelledBk = Pick(BookingStatus.Cancelled);
        var noShowConsultantBk = Pick(BookingStatus.NoShowConsultant);

        // Captured — completed booking, funds captured to the consultant.
        if (completed1 is not null)
        {
            payments.Add(BookingPayment(completed1, PaymentStatus.Captured, p =>
            {
                p.HeldAt = completed1.ConfirmedAt;
                p.CapturedAt = completed1.CompletedAt;
            }));
        }

        // Held — confirmed but the session has not happened yet (escrow).
        if (confirmed is not null)
        {
            payments.Add(BookingPayment(confirmed, PaymentStatus.Held, p => p.HeldAt = confirmed.ConfirmedAt));
        }

        // Pending — booking requested, payment intent created, not captured.
        if (requested is not null)
        {
            payments.Add(BookingPayment(requested, PaymentStatus.Pending));
        }

        // Refunded — student cancelled in time, full refund.
        if (cancelledBk is not null)
        {
            payments.Add(BookingPayment(cancelledBk, PaymentStatus.Refunded, p =>
            {
                p.HeldAt = cancelledBk.ConfirmedAt;
                p.RefundedAt = cancelledBk.CancelledAt;
                p.RefundedAmountCents = p.AmountCents;
                p.RefundReason = "Student cancelled more than 24 hours before the session.";
            }));
        }

        // Disputed — consultant no-show, payment disputed by the student.
        if (noShowConsultantBk is not null)
        {
            payments.Add(BookingPayment(noShowConsultantBk, PaymentStatus.Disputed, p =>
            {
                p.HeldAt = noShowConsultantBk.ConfirmedAt;
                p.FailureReason = "Student opened a dispute after the consultant did not attend.";
            }));
        }

        // Captured #2 — second completed booking (will be settled via a Paid payout).
        if (completed2 is not null)
        {
            payments.Add(BookingPayment(completed2, PaymentStatus.Captured, p =>
            {
                p.HeldAt = completed2.ConfirmedAt;
                p.CapturedAt = completed2.CompletedAt;
            }));
        }

        // ---- company-review payments, remaining statuses ----------------
        // PartiallyRefunded, Failed, Cancelled — attach to scholarships/companies.
        var finalApps = applications
            .Where(a => a.Status is ApplicationStatus.Accepted or ApplicationStatus.Rejected)
            .ToList();
        var anyApp = applications.Count > 0 ? applications[0] : null;
        var company = users.Companies[0].Id;

        if (finalApps.Count > 0)
        {
            // PartiallyRefunded
            payments.Add(ReviewPayment(finalApps[0], company, 2_500, PaymentStatus.PartiallyRefunded, p =>
            {
                p.CapturedAt = now.AddDays(-19);
                p.RefundedAt = now.AddDays(-12);
                p.RefundedAmountCents = 1_000;
                p.RefundReason = "Partial refund issued after a review-scope adjustment.";
            }));
        }

        if (finalApps.Count > 1)
        {
            // Failed
            payments.Add(ReviewPayment(finalApps[1], company, 4_000, PaymentStatus.Failed, p =>
                p.FailureReason = "The card was declined by the issuing bank."));
        }

        // Cancelled — a review payment voided before capture.
        if (anyApp is not null)
        {
            payments.Add(ReviewPayment(anyApp, users.Companies[1].Id, 3_000, PaymentStatus.Cancelled, p =>
                p.FailureReason = "Payment cancelled before capture — the company withdrew the review request."));
        }

        db.Payments.AddRange(payments);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ---- payouts, one per PayoutStatus ------------------------------
        // Settle the captured booking payments into payouts.
        var capturedBookingPayments = payments
            .Where(p => p is { Type: PaymentType.ConsultantBooking, Status: PaymentStatus.Captured })
            .ToList();

        var payouts = new List<Payout>();

        // Paid — fully settled to the first captured payment's payee.
        if (capturedBookingPayments.Count > 0)
        {
            var src = capturedBookingPayments[0];
            var paid = new Payout
            {
                PayeeUserId = src.PayeeUserId!.Value,
                AmountCents = src.PayeeAmountCents,
                Currency = "USD",
                Status = PayoutStatus.Paid,
                StripePayoutId = $"po_demo_{Guid.NewGuid():N}",
                StripeConnectAccountId = "acct_demo_consultant_0",
                InitiatedAt = now.AddDays(-9),
                PaidAt = now.AddDays(-7),
                IncludedPaymentIdsJson = $"[\"{src.Id}\"]",
                CreatedAt = now.AddDays(-9),
            };
            payouts.Add(paid);
            src.PayoutId = paid.Id;
        }

        // InTransit — second captured payment, payout on its way.
        if (capturedBookingPayments.Count > 1)
        {
            var src = capturedBookingPayments[1];
            var inTransit = new Payout
            {
                PayeeUserId = src.PayeeUserId!.Value,
                AmountCents = src.PayeeAmountCents,
                Currency = "USD",
                Status = PayoutStatus.InTransit,
                StripePayoutId = $"po_demo_{Guid.NewGuid():N}",
                StripeConnectAccountId = "acct_demo_consultant_3",
                InitiatedAt = now.AddDays(-1),
                IncludedPaymentIdsJson = $"[\"{src.Id}\"]",
                CreatedAt = now.AddDays(-1),
            };
            payouts.Add(inTransit);
            src.PayoutId = inTransit.Id;
        }

        // Pending — a payout queued but not yet initiated.
        payouts.Add(new Payout
        {
            PayeeUserId = users.Consultants[1].Id,
            AmountCents = 5_400,
            Currency = "USD",
            Status = PayoutStatus.Pending,
            StripeConnectAccountId = "acct_demo_consultant_1",
            CreatedAt = now.AddHours(-3),
        });

        // Failed — a payout that bounced.
        payouts.Add(new Payout
        {
            PayeeUserId = users.Companies[0].Id,
            AmountCents = 2_125,
            Currency = "USD",
            Status = PayoutStatus.Failed,
            StripePayoutId = $"po_demo_{Guid.NewGuid():N}",
            StripeConnectAccountId = "acct_demo_company_0",
            InitiatedAt = now.AddDays(-4),
            FailureReason = "The destination bank account could not be reached.",
            CreatedAt = now.AddDays(-4),
        });

        db.Payouts.AddRange(payouts);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        // ---- company reviews + their payments ---------------------------
        // A CompanyReview is keyed 1:1 to a finalised application (unique index).
        var reviews = new List<CompanyReview>();
        var reviewPayments = new List<CompanyReviewPayment>();
        if (finalApps.Count > 0)
        {
            var app0 = finalApps[0];
            reviews.Add(new CompanyReview
            {
                ApplicationTrackerId = app0.Id,
                StudentId = app0.StudentId,
                CompanyId = users.Companies[0].Id,
                Rating = 5,
                Comment = "The company was responsive and the decision came through quickly. Highly recommended.",
                CreatedAt = now.AddDays(-18),
            });
            reviewPayments.Add(new CompanyReviewPayment
            {
                ApplicationTrackerId = app0.Id,
                CompanyId = users.Companies[0].Id,
                AmountUsd = 25m,
                ProfitShareAmountUsd = 3.75m,
                PayeeAmountUsd = 21.25m,
                StripePaymentIntentId = $"pi_demo_crp_{Guid.NewGuid():N}",
                IdempotencyKey = NextKey("crp"),
                Status = PaymentStatus.Captured,
                CapturedAt = now.AddDays(-19),
            });
        }

        if (finalApps.Count > 1)
        {
            var app1 = finalApps[1];
            reviews.Add(new CompanyReview
            {
                ApplicationTrackerId = app1.Id,
                StudentId = app1.StudentId,
                CompanyId = users.Companies[1].Id,
                Rating = 2,
                Comment = "The rejection feedback was vague. Hidden by an admin pending a content check.",
                IsHiddenByAdmin = true,
                AdminNote = "Temporarily hidden while we verify the claim.",
                CreatedAt = now.AddDays(-13),
            });
            reviewPayments.Add(new CompanyReviewPayment
            {
                ApplicationTrackerId = app1.Id,
                CompanyId = users.Companies[1].Id,
                AmountUsd = 30m,
                ProfitShareAmountUsd = 4.5m,
                PayeeAmountUsd = 25.5m,
                StripePaymentIntentId = $"pi_demo_crp_{Guid.NewGuid():N}",
                IdempotencyKey = NextKey("crp"),
                Status = PaymentStatus.Refunded,
                CapturedAt = now.AddDays(-14),
                RefundedAmountUsd = 30m,
                RefundReason = "Refunded after the review was withdrawn.",
            });
        }

        if (reviews.Count > 0)
        {
            db.CompanyReviews.AddRange(reviews);
            db.CompanyReviewPayments.AddRange(reviewPayments);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        logger.LogInformation(
            "Seeded payments: {P} payments (all statuses, both types), {O} payouts (all statuses), {R} company reviews",
            payments.Count, payouts.Count, reviews.Count);
    }
}
