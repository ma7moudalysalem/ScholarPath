using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.RefundPayment;
using ScholarPath.Domain.Entities;
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

    [Fact]
    public async Task Returns_false_when_payment_not_refundable()
    {
      using  var db = CreateDb();
        var payment = MakePayment(PaymentStatus.Refunded);
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        var sut = new RefundPaymentCommandHandler(
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
            db, stripe, NullLogger<RefundPaymentCommandHandler>.Instance);

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
}
