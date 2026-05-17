using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
        new(db, NullLogger<ProcessStripeWebhookCommandHandler>.Instance);

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
}
