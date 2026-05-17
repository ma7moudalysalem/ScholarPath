using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.CapturePaymentIntent;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public class CapturePaymentIntentCommandHandlerTests
{
    private static ApplicationDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Payment MakeHeldPayment() => new()
    {
        Id = Guid.NewGuid(),
        Status = PaymentStatus.Held,
        AmountCents = 5000,
        Currency = "USD",
        StripePaymentIntentId = $"pi_test_{Guid.NewGuid():N}",
        IdempotencyKey = $"key_{Guid.NewGuid():N}",
        PayerUserId = Guid.NewGuid(),
    };

    [Fact]
    public async Task Returns_false_when_no_held_payment_found()
    {
       using var db = CreateDb();
        var stripe = Substitute.For<IStripeService>();
        var sut = new CapturePaymentIntentCommandHandler(
            db, stripe, NullLogger<CapturePaymentIntentCommandHandler>.Instance);

        var result = await sut.Handle(
            new CapturePaymentIntentCommand(Guid.NewGuid()), default);

        result.Should().BeFalse();
        await stripe.DidNotReceiveWithAnyArgs()
            .CapturePaymentIntentAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task Captures_payment_when_stripe_returns_succeeded()
    {
      using  var db = CreateDb();
        var payment = MakeHeldPayment();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "succeeded", null, "ch_123"));

        var sut = new CapturePaymentIntentCommandHandler(
            db, stripe, NullLogger<CapturePaymentIntentCommandHandler>.Instance);

        var result = await sut.Handle(
            new CapturePaymentIntentCommand(payment.Id), default);

        result.Should().BeTrue();
        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.Status.Should().Be(PaymentStatus.Captured);
        updated.CapturedAt.Should().NotBeNull();
        updated.StripeChargeId.Should().Be("ch_123");
    }

    [Fact]
    public async Task Throws_ConflictException_when_stripe_returns_non_success()
    {
       using var db = CreateDb();
        var payment = MakeHeldPayment();
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "requires_action", null, null));

        var sut = new CapturePaymentIntentCommandHandler(
            db, stripe, NullLogger<CapturePaymentIntentCommandHandler>.Instance);

        var act = () => sut.Handle(
            new CapturePaymentIntentCommand(payment.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*requires_action*");
    }

    [Fact]
    public async Task Returns_false_when_payment_already_captured()
    {
       using var db = CreateDb();
        var payment = MakeHeldPayment();
        payment.Status = PaymentStatus.Captured;
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        var sut = new CapturePaymentIntentCommandHandler(
            db, stripe, NullLogger<CapturePaymentIntentCommandHandler>.Instance);

        var result = await sut.Handle(
            new CapturePaymentIntentCommand(payment.Id), default);

        result.Should().BeFalse();
    }

    [Fact]
    public void Validator_fails_on_empty_id()
    {
        var v = new CapturePaymentIntentCommandValidator();
        v.Validate(new CapturePaymentIntentCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_passes_on_valid_id()
    {
        var v = new CapturePaymentIntentCommandValidator();
        v.Validate(new CapturePaymentIntentCommand(Guid.NewGuid()))
            .IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task Capture_writes_profit_share_snapshot_from_active_config()
    {
        using var db = CreateDb();
        db.ProfitShareConfigs.Add(new ProfitShareConfig
        {
            Id = Guid.NewGuid(),
            PaymentType = PaymentType.ConsultantBooking,
            Percentage = 0.20m,
            EffectiveFrom = DateTimeOffset.UtcNow.AddDays(-1),
            EffectiveTo = null,
            SetByAdminId = Guid.NewGuid(),
        });
        var payment = MakeHeldPayment();   // AmountCents = 5000, Type defaults to ConsultantBooking
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        var stripe = Substitute.For<IStripeService>();
        stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                payment.StripePaymentIntentId!, "succeeded", null, "ch_x"));

        await new CapturePaymentIntentCommandHandler(
                db, stripe, NullLogger<CapturePaymentIntentCommandHandler>.Instance)
            .Handle(new CapturePaymentIntentCommand(payment.Id), default);

        var updated = await db.Payments.FindAsync(payment.Id);
        updated!.ProfitShareAmountCents.Should().Be(1000);   // 20% of 5000
        updated.PayeeAmountCents.Should().Be(4000);
    }
}
