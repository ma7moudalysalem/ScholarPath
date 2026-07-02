using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Accept;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

public class AcceptScholarshipProviderReviewRequestCommandHandlerTests
{
    private static IStripeService StripeCapture(string status = "succeeded")
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StripePaymentIntentResult(
                (string)ci[0], status, null, "ch_test"));
        return stripe;
    }

    [Fact]
    public async Task Captures_payment_and_transitions_to_under_review_with_10_90_split()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending, amountCents: 10_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);
        var stripe = StripeCapture();
        var dispatcher = Substitute.For<INotificationDispatcher>();

        var sut = new AcceptScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser, dispatcher,
            NullLogger<AcceptScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new AcceptScholarshipProviderReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        var updated = db.ScholarshipProviderReviewRequests.Single(r => r.Id == request.Id);
        updated.Status.Should().Be(ScholarshipProviderReviewRequestStatus.UnderReview);
        updated.AcceptedAt.Should().NotBeNull();

        var payment = db.Payments.Single(p => p.Id == updated.PaymentId);
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.CapturedAt.Should().NotBeNull();
        // 10/90 split locked at capture time from the rule in force.
        payment.ProfitShareAmountCents.Should().Be(1_000);
        payment.PayeeAmountCents.Should().Be(9_000);

        await dispatcher.Received().DispatchAsync(
            request.StudentId,
            NotificationType.ScholarshipProviderReviewRequestPaymentCaptured,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await dispatcher.Received().DispatchAsync(
            request.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestPaymentCaptured,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Notification_failure_does_not_block_status_transition()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending, amountCents: 5_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var dispatcher = Substitute.For<INotificationDispatcher>();
        dispatcher.DispatchAsync(
                Arg.Any<Guid>(), Arg.Any<NotificationType>(),
                Arg.Any<Application.Notifications.NotificationParams>(),
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("SMTP down")));

        var sut = new AcceptScholarshipProviderReviewRequestCommandHandler(
            db, StripeCapture(), currentUser, dispatcher,
            NullLogger<AcceptScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new AcceptScholarshipProviderReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        db.ScholarshipProviderReviewRequests.Single().Status
            .Should().Be(ScholarshipProviderReviewRequestStatus.UnderReview);
        db.Payments.Single().Status.Should().Be(PaymentStatus.Captured);
    }

    [Fact]
    public async Task Rejects_non_owning_company()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid()); // somebody else

        var sut = new AcceptScholarshipProviderReviewRequestCommandHandler(
            db, StripeCapture(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<AcceptScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new AcceptScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Rejects_when_status_is_not_pending()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Submitted);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var sut = new AcceptScholarshipProviderReviewRequestCommandHandler(
            db, StripeCapture(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<AcceptScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new AcceptScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Pending*");
    }

    [Fact]
    public async Task Idempotent_when_already_under_review()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.UnderReview);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);
        var stripe = StripeCapture();

        var sut = new AcceptScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<AcceptScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new AcceptScholarshipProviderReviewRequestCommand(request.Id), default);
        result.Should().BeFalse();
        await stripe.DidNotReceiveWithAnyArgs()
            .CapturePaymentIntentAsync(default!, default, default!, default);
    }
}
