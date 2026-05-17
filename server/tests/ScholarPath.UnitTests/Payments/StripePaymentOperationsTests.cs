using FluentAssertions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments;
using Xunit;

namespace ScholarPath.UnitTests.Payments;

public sealed class StripePaymentOperationsTests
{
    private static IStripeService StripeReturning(string status)
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult("pi_1", status, null, null));
        return stripe;
    }

    [Fact]
    public async Task Cancels_with_the_requested_by_customer_reason_and_the_callers_key()
    {
        var stripe = StripeReturning("canceled");

        await stripe.CancelHeldPaymentAsync("pi_1", "refund:abc:cancel", default);

        await stripe.Received(1).CancelPaymentIntentAsync(
            "pi_1", "requested_by_customer", "refund:abc:cancel", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_throw_when_Stripe_confirms_the_cancellation()
    {
        var stripe = StripeReturning("canceled");

        var act = () => stripe.CancelHeldPaymentAsync("pi_1", "key", default);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("requires_capture")]
    [InlineData("succeeded")]
    [InlineData("processing")]
    public async Task Throws_when_Stripe_does_not_confirm_the_cancellation(string status)
    {
        var stripe = StripeReturning(status);

        var act = () => stripe.CancelHeldPaymentAsync("pi_1", "key", default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage($"*{status}*");
    }
}
