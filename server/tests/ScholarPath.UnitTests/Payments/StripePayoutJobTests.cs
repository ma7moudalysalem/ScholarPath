using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Jobs;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public class StripePayoutJobTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static UserProfile Payee(Guid userId, StripeConnectStatus status) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        StripeConnectAccountId = $"acct_{Guid.NewGuid():N}",
        StripeConnectStatus = status,
    };

    private static Payment CapturedPayment(Guid payeeId, long payeeCents) => new()
    {
        Id = Guid.NewGuid(),
        Type = PaymentType.ConsultantBooking,
        Status = PaymentStatus.Captured,
        AmountCents = payeeCents + 100,
        Currency = "USD",
        PayeeAmountCents = payeeCents,
        ProfitShareAmountCents = 100,
        IdempotencyKey = $"key_{Guid.NewGuid():N}",
        PayerUserId = Guid.NewGuid(),
        PayeeUserId = payeeId,
        CapturedAt = DateTimeOffset.UtcNow,
    };

    private static StripePayoutJob Sut(
        ApplicationDbContext db, IStripeService stripe, INotificationDispatcher notif) =>
        new(db, stripe, notif, NullLogger<StripePayoutJob>.Instance);

    private static IStripeService StripeReturning(string status)
    {
        var s = Substitute.For<IStripeService>();
        s.CreatePayoutAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePayoutResult($"po_{Guid.NewGuid():N}", status));
        return s;
    }

    [Fact]
    public async Task Pays_out_verified_payee_and_links_payments()
    {
        using var db = CreateDb();
        var payeeId = Guid.NewGuid();
        db.UserProfiles.Add(Payee(payeeId, StripeConnectStatus.Verified));
        var p1 = CapturedPayment(payeeId, 3000);
        var p2 = CapturedPayment(payeeId, 2000);
        db.Payments.AddRange(p1, p2);
        await db.SaveChangesAsync();

        await Sut(db, StripeReturning("in_transit"), Substitute.For<INotificationDispatcher>())
            .RunAsync(default);

        var payout = db.Payouts.Single();
        payout.PayeeUserId.Should().Be(payeeId);
        payout.AmountCents.Should().Be(5000);
        payout.Status.Should().Be(PayoutStatus.InTransit);

        (await db.Payments.FindAsync(p1.Id))!.PayoutId.Should().Be(payout.Id);
        (await db.Payments.FindAsync(p2.Id))!.PayoutId.Should().Be(payout.Id);
    }

    [Fact]
    public async Task Skips_unverified_payee()
    {
        using var db = CreateDb();
        var payeeId = Guid.NewGuid();
        db.UserProfiles.Add(Payee(payeeId, StripeConnectStatus.Pending));
        var p = CapturedPayment(payeeId, 4000);
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await Sut(db, StripeReturning("in_transit"), Substitute.For<INotificationDispatcher>())
            .RunAsync(default);

        db.Payouts.Should().BeEmpty();
        (await db.Payments.FindAsync(p.Id))!.PayoutId.Should().BeNull();
    }

    [Fact]
    public async Task Does_not_repay_already_paid_out_payments()
    {
        using var db = CreateDb();
        var payeeId = Guid.NewGuid();
        db.UserProfiles.Add(Payee(payeeId, StripeConnectStatus.Verified));
        var p = CapturedPayment(payeeId, 4000);
        p.PayoutId = Guid.NewGuid();
        db.Payments.Add(p);
        await db.SaveChangesAsync();

        await Sut(db, StripeReturning("in_transit"), Substitute.For<INotificationDispatcher>())
            .RunAsync(default);

        db.Payouts.Should().BeEmpty();
    }

    [Fact]
    public async Task Marks_payout_failed_when_stripe_throws()
    {
        using var db = CreateDb();
        var payeeId = Guid.NewGuid();
        db.UserProfiles.Add(Payee(payeeId, StripeConnectStatus.Verified));
        db.Payments.Add(CapturedPayment(payeeId, 4000));
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePayoutAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("stripe down"));

        await Sut(db, stripe, Substitute.For<INotificationDispatcher>()).RunAsync(default);

        var payout = db.Payouts.Single();
        payout.Status.Should().Be(PayoutStatus.Failed);
        payout.FailureReason.Should().Contain("stripe down");
    }

    [Fact]
    public async Task Stripe_failure_releases_payments_for_next_run()
    {
        // PB-013 recovery: pre-claimed payments must be released when Stripe
        // rejects the payout, otherwise PayoutId != null prevents future runs
        // from ever paying them again.
        using var db = CreateDb();
        var payeeId = Guid.NewGuid();
        db.UserProfiles.Add(Payee(payeeId, StripeConnectStatus.Verified));
        var p1 = CapturedPayment(payeeId, 4000);
        var p2 = CapturedPayment(payeeId, 1500);
        db.Payments.AddRange(p1, p2);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePayoutAsync(Arg.Any<string>(), Arg.Any<long>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("stripe down"));

        await Sut(db, stripe, Substitute.For<INotificationDispatcher>()).RunAsync(default);

        (await db.Payments.FindAsync(p1.Id))!.PayoutId.Should().BeNull();
        (await db.Payments.FindAsync(p2.Id))!.PayoutId.Should().BeNull();
        db.Payouts.Single().Status.Should().Be(PayoutStatus.Failed);
    }
}
