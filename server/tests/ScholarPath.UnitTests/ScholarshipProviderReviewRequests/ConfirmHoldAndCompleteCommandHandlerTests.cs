using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Complete;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.ConfirmHold;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

public class ConfirmHoldAndCompleteCommandHandlerTests
{
    [Fact]
    public async Task ConfirmHold_transitions_submitted_to_pending_and_payment_to_held()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Submitted);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        var dispatcher = Substitute.For<INotificationDispatcher>();

        var sut = new ConfirmScholarshipProviderReviewRequestHoldCommandHandler(
            db, currentUser, dispatcher,
            NullLogger<ConfirmScholarshipProviderReviewRequestHoldCommandHandler>.Instance);

        var result = await sut.Handle(
            new ConfirmScholarshipProviderReviewRequestHoldCommand(request.Id), default);

        result.Should().BeTrue();
        db.ScholarshipProviderReviewRequests.Single().Status.Should().Be(ScholarshipProviderReviewRequestStatus.Pending);
        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Held);
        payment.HeldAt.Should().NotBeNull();

        await dispatcher.Received().DispatchAsync(
            request.StudentId,
            NotificationType.ScholarshipProviderReviewRequestPaymentHeld,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await dispatcher.Received().DispatchAsync(
            request.ScholarshipProviderId,
            NotificationType.ScholarshipProviderReviewRequestIncoming,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmHold_is_idempotent_when_already_pending()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new ConfirmScholarshipProviderReviewRequestHoldCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConfirmScholarshipProviderReviewRequestHoldCommandHandler>.Instance);

        var result = await sut.Handle(
            new ConfirmScholarshipProviderReviewRequestHoldCommand(request.Id), default);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmHold_rejects_non_student_caller()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Submitted);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid()); // not the student

        var sut = new ConfirmScholarshipProviderReviewRequestHoldCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConfirmScholarshipProviderReviewRequestHoldCommandHandler>.Instance);

        var act = () => sut.Handle(
            new ConfirmScholarshipProviderReviewRequestHoldCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Complete_transitions_under_review_to_completed_and_retains_capture()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.UnderReview, amountCents: 12_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var sut = new CompleteScholarshipProviderReviewRequestCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CompleteScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new CompleteScholarshipProviderReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        var updated = db.ScholarshipProviderReviewRequests.Single();
        updated.Status.Should().Be(ScholarshipProviderReviewRequestStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
        // Spec PART 12: completed retains the captured payment, no refund.
        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.RefundedAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Complete_rejects_non_under_review_status()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var sut = new CompleteScholarshipProviderReviewRequestCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CompleteScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new CompleteScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*UnderReview*");
    }
}
