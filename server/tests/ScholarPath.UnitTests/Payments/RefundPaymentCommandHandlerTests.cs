using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.RefundPayment;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public class RefundPaymentCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Payment MakePayment(
        PaymentStatus status,
        long amountCents = 5000) => new()
        {
            Id = Guid.NewGuid(),
            Status = status,
            AmountCents = amountCents,
            RefundedAmountCents = 0,
            Currency = "USD",
            StripePaymentIntentId = $"pi_test_{Guid.NewGuid():N}",
            IdempotencyKey = $"key_{Guid.NewGuid():N}",
            PayerUserId = Guid.NewGuid(),
        };

    private static ICurrentUserService AdminUser()
    {
        var user = Substitute.For<ICurrentUserService>();
        user.IsInRole("Admin").Returns(true);
        return user;
    }

    [Fact]
    public async Task Returns_false_when_payment_not_refundable()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Refunded);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var result = await sut.Handle(
            new RefundPaymentCommand(payment.Id, null, null), default);

        result.Should().BeFalse();
        await stripe.DidNotReceiveWithAnyArgs()
            .CancelPaymentIntentAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Full_refund_on_held_sets_status_refunded()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Held, 5000);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "canceled", null, null));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var result = await sut.Handle(
            new RefundPaymentCommand(payment.Id, null, "full refund"), default);

        result.Should().BeTrue();
        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Refunded);
        updated.RefundedAmountCents.Should().Be(5000);
        updated.RefundedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Partial_refund_on_captured_sets_status_partially_refunded()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 10000);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_test", "succeeded", 3000));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var result = await sut.Handle(
            new RefundPaymentCommand(payment.Id, 3000, "partial"), default);

        result.Should().BeTrue();
        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        updated.RefundedAmountCents.Should().Be(3000);
    }

    [Fact]
    public async Task Full_refund_on_captured_sets_status_refunded()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 5000);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_full", "succeeded", 5000));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var result = await sut.Handle(
            new RefundPaymentCommand(payment.Id, null, "full"), default);

        result.Should().BeTrue();
        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Refunded);
        updated.RefundedAmountCents.Should().Be(5000);
    }

    [Fact]
    public async Task Throws_when_stripe_cancel_fails()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Held);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "requires_action", null, null));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var act = () => sut.Handle(
            new RefundPaymentCommand(payment.Id, null, null), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*requires_action*");
    }

    [Fact]
    public async Task Throws_when_stripe_refund_fails()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 5000);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_fail", "pending", 0));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var act = () => sut.Handle(
            new RefundPaymentCommand(payment.Id, 3000, null), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*pending*");
    }

    [Fact]
    public void Validator_fails_on_empty_id()
    {
        var v = new RefundPaymentCommandValidator();
        v.Validate(new RefundPaymentCommand(Guid.Empty, null, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_fails_on_zero_amount()
    {
        var v = new RefundPaymentCommandValidator();
        v.Validate(new RefundPaymentCommand(Guid.NewGuid(), 0, null))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_passes_on_null_amount()
    {
        var v = new RefundPaymentCommandValidator();
        v.Validate(new RefundPaymentCommand(Guid.NewGuid(), null, null))
            .IsValid.Should().BeTrue();
    }

    // ── PB-014 v1: commission applies to RETAINED amount, not to gross ──────

    [Fact]
    public async Task Full_refund_on_Held_zeroes_commission_and_payee()
    {
        using var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Held, 10_000);
        payment.ProfitShareAmountCents = 1_000;
        payment.PayeeAmountCents = 9_000;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "canceled", null, null));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        var ok = await sut.Handle(new RefundPaymentCommand(payment.Id, null, "full"), default);

        ok.Should().BeTrue();
        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.ProfitShareAmountCents.Should().Be(0);
        updated.PayeeAmountCents.Should().Be(0);
        updated.RefundedAmountCents.Should().Be(10_000);
    }

    [Fact]
    public async Task Full_refund_on_Captured_zeroes_commission_and_payee()
    {
        using var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 10_000);
        payment.ProfitShareAmountCents = 1_000;
        payment.PayeeAmountCents = 9_000;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_full", "succeeded", 10_000));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        await sut.Handle(new RefundPaymentCommand(payment.Id, null, "full"), default);

        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Refunded);
        updated.RefundedAmountCents.Should().Be(10_000);
        updated.ProfitShareAmountCents.Should().Be(0);
        updated.PayeeAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Partial_refund_50pct_recomputes_commission_from_retained()
    {
        // Gross 10000, refund 5000 → retained 5000.
        // Default v1 split = 10% / 90% → commission 500, payee 4500.
        using var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 10_000);
        payment.ProfitShareAmountCents = 1_000;
        payment.PayeeAmountCents = 9_000;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_half", "succeeded", 5_000));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        await sut.Handle(new RefundPaymentCommand(payment.Id, 5_000, "partial"), default);

        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        updated.RefundedAmountCents.Should().Be(5_000);
        updated.ProfitShareAmountCents.Should().Be(500);
        updated.PayeeAmountCents.Should().Be(4_500);
        (updated.RefundedAmountCents + updated.ProfitShareAmountCents + updated.PayeeAmountCents)
            .Should().Be(updated.AmountCents);
    }

    [Fact]
    public async Task Partial_refund_30pct_recomputes_commission_from_retained()
    {
        // Gross 10000, refund 3000 → retained 7000.
        // 10% commission = 700, payee = 6300.
        using var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 10_000);
        payment.ProfitShareAmountCents = 1_000;
        payment.PayeeAmountCents = 9_000;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_30", "succeeded", 3_000));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        await sut.Handle(new RefundPaymentCommand(payment.Id, 3_000, "30pct"), default);

        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.ProfitShareAmountCents.Should().Be(700);
        updated.PayeeAmountCents.Should().Be(6_300);
    }

    [Fact]
    public async Task Tiny_amount_refund_handles_rounding_safely()
    {
        // Gross 1c, fully refunded → retained 0 → commission 0, payee 0.
        using var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Captured, 1);
        payment.ProfitShareAmountCents = 0;
        payment.PayeeAmountCents = 1;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string?>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripeRefundResult("re_tiny", "succeeded", 1));

        var sut = new RefundPaymentCommandHandler(
            db, stripe, AdminUser(), NullLogger<RefundPaymentCommandHandler>.Instance);

        await sut.Handle(new RefundPaymentCommand(payment.Id, null, null), default);

        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Refunded);
        updated.ProfitShareAmountCents.Should().Be(0);
        updated.PayeeAmountCents.Should().Be(0);
    }
}
