using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Cancel;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

public class CancelScholarshipProviderReviewRequestCommandHandlerTests
{
    private static IStripeService StripeOk()
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StripePaymentIntentResult(
                (string)ci[0], "canceled", null, null));
        stripe.RefundPaymentAsync(
                Arg.Any<string>(), Arg.Any<long>(),
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(ci => new StripeRefundResult(
                $"re_{Guid.NewGuid():N}", "succeeded", (long)ci[1]));
        return stripe;
    }

    [Fact]
    public async Task Cancel_from_pending_releases_hold_and_does_not_charge()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending, amountCents: 10_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        var stripe = StripeOk();
        var dispatcher = Substitute.For<INotificationDispatcher>();

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser, dispatcher,
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new CancelScholarshipProviderReviewRequestCommand(request.Id, "Changed my mind"), default);

        result.Should().BeTrue();
        var updated = db.ScholarshipProviderReviewRequests.Single();
        updated.Status.Should().Be(ScholarshipProviderReviewRequestStatus.CancelledByStudent);

        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.RefundedAmountCents.Should().Be(0, "spec: cancel-from-Pending charges nothing");
        // No refund API call — only the hold cancellation.
        await stripe.DidNotReceiveWithAnyArgs()
            .RefundPaymentAsync(default!, default, default!, default!, default);
        await stripe.Received(1).CancelPaymentIntentAsync(
            payment.StripePaymentIntentId!, Arg.Any<string>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        await dispatcher.Received().DispatchAsync(
            request.StudentId,
            NotificationType.ScholarshipProviderReviewRequestPaymentHoldCancelled,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Cancel_from_under_review_applies_50pct_refund_and_relocks_split_from_retained()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        // Gross 10,000c (=$100). 50% refund -> 5,000c retained. Commission 10%
        // of retained = 500c; ScholarshipProvider share 4,500c (spec PART 6).
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.UnderReview, amountCents: 10_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        var stripe = StripeOk();

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new CancelScholarshipProviderReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        var updated = db.ScholarshipProviderReviewRequests.Single();
        updated.Status.Should().Be(ScholarshipProviderReviewRequestStatus.CancelledByStudent);

        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.PartiallyRefunded);
        payment.RefundedAmountCents.Should().Be(5_000);
        // Re-locked from RETAINED amount, not the original gross.
        payment.ProfitShareAmountCents.Should().Be(500);
        payment.PayeeAmountCents.Should().Be(4_500);
    }

    [Fact]
    public async Task Cancel_after_completed_is_rejected()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Completed);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new CancelScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*already completed*");
    }

    [Fact]
    public async Task Cancel_is_idempotent_on_already_cancelled_request()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.CancelledByStudent);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new CancelScholarshipProviderReviewRequestCommand(request.Id), default);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Cancel_rejects_non_owning_student()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid()); // another student

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new CancelScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Refund_math_handles_odd_cent_amounts_without_drift()
    {
        // 999c gross — half-up rounding gives 500c refund + 499c retained,
        // re-summing exactly to gross. Commission = round(499 * 0.10) = 50c.
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.UnderReview, amountCents: 999);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new CancelScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelScholarshipProviderReviewRequestCommandHandler>.Instance);

        await sut.Handle(new CancelScholarshipProviderReviewRequestCommand(request.Id), default);

        var payment = db.Payments.Single();
        payment.RefundedAmountCents.Should().Be(500);
        (payment.AmountCents - payment.RefundedAmountCents).Should().Be(499);
        payment.ProfitShareAmountCents.Should().Be(50);
        payment.PayeeAmountCents.Should().Be(449);
    }
}
