using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Commands.Complete;
using ScholarPath.Application.CompanyReviewRequests.Commands.ConfirmHold;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviewRequests;

public class ConfirmHoldAndCompleteCommandHandlerTests
{
    [Fact]
    public async Task ConfirmHold_transitions_submitted_to_pending_and_payment_to_held()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Submitted);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        var dispatcher = Substitute.For<INotificationDispatcher>();

        var sut = new ConfirmCompanyReviewRequestHoldCommandHandler(
            db, currentUser, dispatcher,
            NullLogger<ConfirmCompanyReviewRequestHoldCommandHandler>.Instance);

        var result = await sut.Handle(
            new ConfirmCompanyReviewRequestHoldCommand(request.Id), default);

        result.Should().BeTrue();
        db.CompanyReviewRequests.Single().Status.Should().Be(CompanyReviewRequestStatus.Pending);
        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Held);
        payment.HeldAt.Should().NotBeNull();

        await dispatcher.Received().DispatchAsync(
            request.StudentId,
            NotificationType.CompanyReviewRequestPaymentHeld,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
        await dispatcher.Received().DispatchAsync(
            request.CompanyId,
            NotificationType.CompanyReviewRequestIncoming,
            Arg.Any<Application.Notifications.NotificationParams>(),
            Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ConfirmHold_is_idempotent_when_already_pending()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new ConfirmCompanyReviewRequestHoldCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConfirmCompanyReviewRequestHoldCommandHandler>.Instance);

        var result = await sut.Handle(
            new ConfirmCompanyReviewRequestHoldCommand(request.Id), default);
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ConfirmHold_rejects_non_student_caller()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Submitted);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid()); // not the student

        var sut = new ConfirmCompanyReviewRequestHoldCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConfirmCompanyReviewRequestHoldCommandHandler>.Instance);

        var act = () => sut.Handle(
            new ConfirmCompanyReviewRequestHoldCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Complete_transitions_under_review_to_completed_and_retains_capture()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.UnderReview, amountCents: 12_000);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new CompleteCompanyReviewRequestCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CompleteCompanyReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new CompleteCompanyReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        var updated = db.CompanyReviewRequests.Single();
        updated.Status.Should().Be(CompanyReviewRequestStatus.Completed);
        updated.CompletedAt.Should().NotBeNull();
        // Spec PART 12: completed retains the captured payment, no refund.
        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Captured);
        payment.RefundedAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Complete_rejects_non_under_review_status()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new CompleteCompanyReviewRequestCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CompleteCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new CompleteCompanyReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*UnderReview*");
    }
}
