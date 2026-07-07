using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Expire;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Reject;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

public class RejectAndExpireCommandHandlerTests
{
    private static IStripeService StripeOk()
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CancelPaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StripePaymentIntentResult(
                (string)ci[0], "canceled", null, null));
        return stripe;
    }

    [Fact]
    public async Task Reject_cancels_hold_and_no_amount_is_captured()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending, amountCents: 7_500);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var sut = new RejectScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new RejectScholarshipProviderReviewRequestCommand(request.Id, "Out of scope"), default);

        result.Should().BeTrue();
        var updated = db.ScholarshipProviderReviewRequests.Single();
        updated.Status.Should().Be(ScholarshipProviderReviewRequestStatus.RejectedByScholarshipProvider);
        updated.RejectedAt.Should().NotBeNull();
        updated.RejectReason.Should().Be("Out of scope");

        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.RefundedAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Reject_rejects_non_pending_status()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.UnderReview);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.ScholarshipProviderId);

        var sut = new RejectScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new RejectScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Pending*");
    }

    // Business rule: a provider must give a reason when rejecting a paid support
    // request (the student always learns why). The validator enforces it.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validator_requires_a_reject_reason(string? reason)
    {
        var validator = new RejectScholarshipProviderReviewRequestCommandValidator();
        var result = validator.TestValidate(
            new RejectScholarshipProviderReviewRequestCommand(Guid.NewGuid(), reason));
        result.ShouldHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public void Validator_accepts_a_non_empty_reject_reason()
    {
        var validator = new RejectScholarshipProviderReviewRequestCommandValidator();
        var result = validator.TestValidate(
            new RejectScholarshipProviderReviewRequestCommand(Guid.NewGuid(), "Documents incomplete"));
        result.ShouldNotHaveValidationErrorFor(x => x.Reason);
    }

    [Fact]
    public async Task Expire_cancels_hold_when_admin_invokes_it()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.IsInRole("Admin").Returns(true);

        var sut = new ExpireScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new ExpireScholarshipProviderReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        db.ScholarshipProviderReviewRequests.Single().Status.Should().Be(ScholarshipProviderReviewRequestStatus.Expired);
        db.Payments.Single().Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task Expire_rejects_non_admin_caller_without_skip_owner_check()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        currentUser.IsInRole("Admin").Returns(false);

        var sut = new ExpireScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new ExpireScholarshipProviderReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Expire_allows_system_caller_via_skip_owner_check()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (request, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedRequestWithPayment(db, ScholarshipProviderReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.IsInRole(Arg.Any<string>()).Returns(false);

        var sut = new ExpireScholarshipProviderReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new ExpireScholarshipProviderReviewRequestCommand(request.Id, SkipOwnerCheck: true), default);

        result.Should().BeTrue();
    }
}
