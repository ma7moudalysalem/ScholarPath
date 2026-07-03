using System.Text.Json;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Jobs;

public interface IStripePayoutJob
{
    // RACE-02: the recurring job is registered against THIS interface method
    // (Program.cs AddOrUpdate<IStripePayoutJob>), and Hangfire resolves job-filter
    // attributes from the registered (interface) method — so the concurrency lock
    // must live here, not on the concrete implementation, to actually take effect.
    // A single Hangfire server then never starts two overlapping payout runs; the
    // durable cross-instance guarantee still comes from the pre-claim +
    // Payment.RowVersion concurrency token in the implementation.
    [DisableConcurrentExecution(timeoutInSeconds: 30 * 60)]
    Task RunAsync(CancellationToken ct);
}

/// <summary>
/// Nightly batch payout (PB-013). Groups every captured-but-not-yet-paid-out payment
/// by payee and sends one Stripe payout per payee for the sum of PayeeAmountCents.
/// Payees whose Stripe Connect account is not verified are skipped (retried next run).
/// </summary>
public sealed class StripePayoutJob(
    ApplicationDbContext db,
    IStripeService stripeService,
    INotificationDispatcher notifications,
    ILogger<StripePayoutJob> logger) : IStripePayoutJob
{
    // RACE-02: the [DisableConcurrentExecution] lock lives on IStripePayoutJob.RunAsync
    // (Hangfire reads filters off the registered interface method). The durable
    // cross-instance guarantee is the pre-claim + Payment.RowVersion token below.
    public async Task RunAsync(CancellationToken ct)
    {
        // Include PartiallyRefunded — consultant earns the kept portion after
        // a partial-refund cancel (FR-090). The CancelBooking handler recomputes
        // PayeeAmountCents to be (gross - refunded - profitShare).
        var pending = await db.Payments
            .Where(p => (p.Status == PaymentStatus.Captured || p.Status == PaymentStatus.PartiallyRefunded)
                && p.PayoutId == null
                && p.PayeeUserId != null
                && p.PayeeAmountCents > 0)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (pending.Count == 0)
        {
            logger.LogInformation("[payout] nothing to pay out.");
            return;
        }

        var byPayee = pending.GroupBy(p => p.PayeeUserId!.Value).ToList();

        var payeeIds = byPayee.Select(g => g.Key).ToList();
        var profiles = await db.UserProfiles
            .Where(up => payeeIds.Contains(up.UserId))
            .ToDictionaryAsync(up => up.UserId, ct)
            .ConfigureAwait(false);

        int paid = 0, skipped = 0, failed = 0;

        foreach (var group in byPayee)
        {
            var payeeId = group.Key;
            var payments = group.ToList();

            // Only a verified Connect account can receive money (PB-013 risk #2).
            if (!profiles.TryGetValue(payeeId, out var profile)
                || string.IsNullOrEmpty(profile.StripeConnectAccountId)
                || profile.StripeConnectStatus != StripeConnectStatus.Verified)
            {
                logger.LogInformation(
                    "[payout] skipping payee {PayeeId} — Connect account not verified.", payeeId);
                skipped++;
                continue;
            }

            var totalCents = payments.Sum(p => p.PayeeAmountCents);
            if (totalCents <= 0) { skipped++; continue; }

            // Claim the payments up front: persisting the Payout row + back-links
            // before calling Stripe guarantees a re-run never pays them twice.
            var payout = new Payout
            {
                Id = Guid.NewGuid(),
                PayeeUserId = payeeId,
                AmountCents = totalCents,
                Currency = "USD",
                Status = PayoutStatus.Pending,
                StripeConnectAccountId = profile.StripeConnectAccountId,
                InitiatedAt = DateTimeOffset.UtcNow,
                IncludedPaymentIdsJson = JsonSerializer.Serialize(payments.Select(p => p.Id)),
            };
            db.Payouts.Add(payout);
            foreach (var p in payments) p.PayoutId = payout.Id;

            try
            {
                // Pre-claim BEFORE calling Stripe so a re-run never pays twice.
                await db.SaveChangesAsync(ct).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException)
            {
                // RACE-02: another instance/run already claimed these payments —
                // the pre-claim UPDATE hit 0 rows via the RowVersion concurrency
                // token, so no double payout happened. Detach this payee's failed
                // edits (the whole SaveChanges rolled back, so nothing was written)
                // so a LATER payee's SaveChanges isn't tripped by the stale tracked
                // rows, then skip to the next payee instead of aborting the run.
                logger.LogInformation(
                    "[payout] payee {PayeeId} already claimed by a concurrent run — skipping.", payeeId);
                db.Entry(payout).State = EntityState.Detached;
                foreach (var p in payments)
                    db.Entry(p).State = EntityState.Detached;
                skipped++;
                continue;
            }

            try
            {
                var result = await stripeService.CreatePayoutAsync(
                    profile.StripeConnectAccountId!, totalCents, "usd",
                    $"payout:{payout.Id:N}", ct).ConfigureAwait(false);

                payout.StripePayoutId = result.Id;
                payout.Status = result.Status == "paid"
                    ? PayoutStatus.Paid
                    : PayoutStatus.InTransit;
                if (payout.Status == PayoutStatus.Paid)
                    payout.PaidAt = DateTimeOffset.UtcNow;

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                logger.LogInformation(
                    "[payout] payee {PayeeId}: {Amount}c over {Count} payment(s) — payout {PayoutId}.",
                    payeeId, totalCents, payments.Count, payout.Id);
                paid++;

                await SafeNotifyAsync(payeeId, NotificationType.PayoutInitiated,
                    new NotificationParams { AmountText = $"${totalCents / 100m:0.00}" }, ct);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[payout] Stripe payout failed for payee {PayeeId}.", payeeId);
                failed++;

                payout.Status = PayoutStatus.Failed;
                payout.FailureReason = Truncate(ex.Message, 500);

                // PB-013 recovery: the payments were pre-claimed (PayoutId set)
                // before we called Stripe. The Stripe call failed, so release
                // them — otherwise the next nightly run will skip them forever
                // because PayoutId != null. Payees with restricted Connect
                // accounts are still gated by the verified-status check above.
                foreach (var p in payments)
                {
                    p.PayoutId = null;
                }

                await db.SaveChangesAsync(ct).ConfigureAwait(false);

                await SafeNotifyAsync(payeeId, NotificationType.PayoutFailed,
                    NotificationParams.Empty, ct);
            }
        }

        logger.LogInformation(
            "[payout] run complete — paid={Paid} skipped={Skipped} failed={Failed}.",
            paid, skipped, failed);
    }

    private async Task SafeNotifyAsync(
        Guid payeeId, NotificationType type, NotificationParams parameters, CancellationToken ct)
    {
        try
        {
            await notifications.DispatchAsync(
                payeeId, type, parameters,
                deepLink: null, idempotencyKey: null, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "[payout] notification dispatch failed for payee {PayeeId}.", payeeId);
        }
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max];
}
