using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Notifications;
using ScholarPath.Application.Payments.Commands.ProcessStripeWebhook;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public class ProcessStripeWebhookConnectTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static ProcessStripeWebhookCommandHandler Sut(ApplicationDbContext db) =>
        new(db, Substitute.For<INotificationDispatcher>(),
            NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

    [Fact]
    public async Task Account_updated_marks_payee_verified_when_payouts_enabled()
    {
        using var db = CreateDb();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StripeConnectAccountId = "acct_abc",
            StripeConnectStatus = StripeConnectStatus.Pending,
        };
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_1", "account.updated", null, null, null, "{}",
            ConnectAccountId: "acct_abc", ConnectPayoutsEnabled: true), default);

        var updated = await db.UserProfiles.FindAsync(profile.Id);
        updated!.StripeConnectStatus.Should().Be(StripeConnectStatus.Verified);
        updated.StripeConnectOnboardedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Account_updated_keeps_payee_pending_when_payouts_disabled()
    {
        using var db = CreateDb();
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            StripeConnectAccountId = "acct_def",
            StripeConnectStatus = StripeConnectStatus.Pending,
        };
        db.UserProfiles.Add(profile);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_2", "account.updated", null, null, null, "{}",
            ConnectAccountId: "acct_def", ConnectPayoutsEnabled: false), default);

        var updated = await db.UserProfiles.FindAsync(profile.Id);
        updated!.StripeConnectStatus.Should().Be(StripeConnectStatus.Pending);
    }

    [Fact]
    public async Task Payout_paid_marks_payout_paid()
    {
        using var db = CreateDb();
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            PayeeUserId = Guid.NewGuid(),
            AmountCents = 5000,
            Currency = "USD",
            Status = PayoutStatus.InTransit,
            StripePayoutId = "po_abc",
        };
        db.Payouts.Add(payout);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_3", "payout.paid", null, null, null, "{}",
            PayoutId: "po_abc"), default);

        var updated = await db.Payouts.FindAsync(payout.Id);
        updated!.Status.Should().Be(PayoutStatus.Paid);
        updated.PaidAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Payout_failed_marks_payout_failed_with_reason()
    {
        using var db = CreateDb();
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            PayeeUserId = Guid.NewGuid(),
            AmountCents = 5000,
            Currency = "USD",
            Status = PayoutStatus.InTransit,
            StripePayoutId = "po_xyz",
        };
        db.Payouts.Add(payout);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_4", "payout.failed", null, null, null, "{}",
            PayoutId: "po_xyz", PayoutFailureMessage: "account closed"), default);

        var updated = await db.Payouts.FindAsync(payout.Id);
        updated!.Status.Should().Be(PayoutStatus.Failed);
        updated.FailureReason.Should().Be("account closed");
    }

    [Fact]
    public async Task Payout_failed_releases_linked_payments_for_retry()
    {
        // PB-013 recovery: when Stripe reports a payout failed, every Payment
        // pre-claimed into it must have PayoutId cleared so the next nightly
        // run can include them in a fresh batch. Otherwise the payments are
        // orphaned forever.
        using var db = CreateDb();
        var payee = Guid.NewGuid();
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            PayeeUserId = payee,
            AmountCents = 7500,
            Currency = "USD",
            Status = PayoutStatus.InTransit,
            StripePayoutId = "po_fail",
        };
        db.Payouts.Add(payout);

        var p1 = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 5000, Currency = "USD",
            PayerUserId = Guid.NewGuid(), PayeeUserId = payee,
            PayeeAmountCents = 4500, ProfitShareAmountCents = 500,
            StripePaymentIntentId = "pi_a",
            IdempotencyKey = "k_a",
            PayoutId = payout.Id,
        };
        var p2 = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 3000, Currency = "USD",
            PayerUserId = Guid.NewGuid(), PayeeUserId = payee,
            PayeeAmountCents = 2700, ProfitShareAmountCents = 300,
            StripePaymentIntentId = "pi_b",
            IdempotencyKey = "k_b",
            PayoutId = payout.Id,
        };
        db.Payments.AddRange(p1, p2);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_release", "payout.failed", null, null, null, "{}",
            PayoutId: "po_fail", PayoutFailureMessage: "account closed"), default);

        var refreshed1 = await db.Payments.FindAsync(p1.Id);
        var refreshed2 = await db.Payments.FindAsync(p2.Id);
        refreshed1!.PayoutId.Should().BeNull();
        refreshed2!.PayoutId.Should().BeNull();

        var refreshedPayout = await db.Payouts.FindAsync(payout.Id);
        refreshedPayout!.Status.Should().Be(PayoutStatus.Failed);
    }

    [Fact]
    public async Task Payout_paid_does_not_release_linked_payments()
    {
        using var db = CreateDb();
        var payout = new Payout
        {
            Id = Guid.NewGuid(),
            PayeeUserId = Guid.NewGuid(),
            AmountCents = 5000, Currency = "USD",
            Status = PayoutStatus.InTransit,
            StripePayoutId = "po_ok",
        };
        db.Payouts.Add(payout);
        var p = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 5000, Currency = "USD",
            PayerUserId = Guid.NewGuid(),
            StripePaymentIntentId = "pi_z", IdempotencyKey = "k_z",
            PayoutId = payout.Id,
        };
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await Sut(db).Handle(new ProcessStripeWebhookCommand(
            "evt_paid", "payout.paid", null, null, null, "{}",
            PayoutId: "po_ok"), default);

        (await db.Payments.FindAsync(p.Id))!.PayoutId.Should().Be(payout.Id);
    }

    [Fact]
    public async Task Charge_dispute_created_marks_payment_disputed_and_alerts_admins()
    {
        using var db = CreateDb();
        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            Type = PaymentType.ConsultantBooking,
            Status = PaymentStatus.Captured,
            AmountCents = 5000,
            Currency = "USD",
            StripeChargeId = "ch_disputed",
            IdempotencyKey = $"key_{Guid.NewGuid():N}",
            PayerUserId = Guid.NewGuid(),
        };
        db.Payments.Add(payment);
        db.Users.Add(new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FirstName = "Admin",
            LastName = "User",
            Email = "admin@scholarpath.local",
            UserName = "admin@scholarpath.local",
            ActiveRole = "Admin",
            AccountStatus = AccountStatus.Active,
        });
        await db.SaveChangesAsync();

        var notifier = Substitute.For<INotificationDispatcher>();
        var sut = new ProcessStripeWebhookCommandHandler(
            db, notifier, NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

        await sut.Handle(new ProcessStripeWebhookCommand(
            "evt_dispute", "charge.dispute.created", null, "ch_disputed", 5000, "{}",
            DisputeReason: "fraudulent"), default);

        (await db.Payments.FindAsync(payment.Id))!.Status.Should().Be(PaymentStatus.Disputed);
        await notifier.Received().DispatchBroadcastAsync(
            Arg.Any<IReadOnlyCollection<Guid>>(),
            NotificationType.PaymentDisputed,
            Arg.Any<NotificationParams>(),
            Arg.Any<CancellationToken>());
    }
}
