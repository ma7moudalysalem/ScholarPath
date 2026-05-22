using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Commands.Expire;
using ScholarPath.Application.CompanyReviewRequests.Commands.Reject;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviewRequests;

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
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending, amountCents: 7_500);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new RejectCompanyReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectCompanyReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new RejectCompanyReviewRequestCommand(request.Id, "Out of scope"), default);

        result.Should().BeTrue();
        var updated = db.CompanyReviewRequests.Single();
        updated.Status.Should().Be(CompanyReviewRequestStatus.RejectedByCompany);
        updated.RejectedAt.Should().NotBeNull();
        updated.RejectReason.Should().Be("Out of scope");

        var payment = db.Payments.Single();
        payment.Status.Should().Be(PaymentStatus.Cancelled);
        payment.RefundedAmountCents.Should().Be(0);
    }

    [Fact]
    public async Task Reject_rejects_non_pending_status()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.UnderReview);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new RejectCompanyReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new RejectCompanyReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ConflictException>().WithMessage("*Pending*");
    }

    [Fact]
    public async Task Expire_cancels_hold_when_admin_invokes_it()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.IsInRole("Admin").Returns(true);

        var sut = new ExpireCompanyReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireCompanyReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(new ExpireCompanyReviewRequestCommand(request.Id), default);

        result.Should().BeTrue();
        db.CompanyReviewRequests.Single().Status.Should().Be(CompanyReviewRequestStatus.Expired);
        db.Payments.Single().Status.Should().Be(PaymentStatus.Cancelled);
    }

    [Fact]
    public async Task Expire_rejects_non_admin_caller_without_skip_owner_check()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);
        currentUser.IsInRole("Admin").Returns(false);

        var sut = new ExpireCompanyReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(new ExpireCompanyReviewRequestCommand(request.Id), default);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Expire_allows_system_caller_via_skip_owner_check()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns((Guid?)null);
        currentUser.IsInRole(Arg.Any<string>()).Returns(false);

        var sut = new ExpireCompanyReviewRequestCommandHandler(
            db, StripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireCompanyReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new ExpireCompanyReviewRequestCommand(request.Id, SkipOwnerCheck: true), default);

        result.Should().BeTrue();
    }
}
