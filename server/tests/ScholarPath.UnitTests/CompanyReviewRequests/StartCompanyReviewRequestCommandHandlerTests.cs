using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Commands.Start;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviewRequests;

public class StartCompanyReviewRequestCommandHandlerTests
{
    private static IStripeService MakeStripeReturningClientSecret(string id = "pi_test_123")
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                id, "requires_payment_method", "cs_test_abc", null));
        return stripe;
    }

    [Fact]
    public void Validator_rejects_empty_scholarship_id()
    {
        var v = new StartCompanyReviewRequestCommandValidator();
        v.Validate(new StartCompanyReviewRequestCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Creates_request_and_manual_capture_intent_for_open_scholarship_with_fee()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = CompanyReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 150m);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = MakeStripeReturningClientSecret("pi_xyz");

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);

        result.AmountCents.Should().Be(15_000);
        result.Currency.Should().Be("USD");

        // Stripe was called with manual capture (PB-005: hold-then-capture-on-accept).
        await stripe.Received(1).CreatePaymentIntentAsync(
            15_000, "usd", "manual",
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        var entity = db.CompanyReviewRequests.Single();
        entity.Status.Should().Be(CompanyReviewRequestStatus.Submitted);
        entity.StudentId.Should().Be(student.Id);
        entity.CompanyId.Should().Be(scholarship.OwnerCompanyId!.Value);
        entity.ReviewFeeUsdSnapshot.Should().Be(150m);
        entity.PendingExpiresAt.Should().NotBeNull();

        var payment = db.Payments.Single();
        payment.Type.Should().Be(PaymentType.CompanyReview);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.AmountCents.Should().Be(15_000);
        // Default 10/90 split locked at submission (re-locked on capture).
        payment.ProfitShareAmountCents.Should().Be(1_500);
        payment.PayeeAmountCents.Should().Be(13_500);
    }

    [Fact]
    public async Task Rejects_when_scholarship_has_no_review_fee()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = CompanyReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: null);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Review Service Fee*");
    }

    [Fact]
    public async Task Rejects_when_company_tries_to_apply_to_its_own_scholarship()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = CompanyReviewRequestTestFixtures
            .SeedParticipants(db);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*own scholarship*");
    }

    [Fact]
    public async Task Returns_existing_submitted_request_on_double_click()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = CompanyReviewRequestTestFixtures
            .SeedParticipants(db);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = MakeStripeReturningClientSecret("pi_only_once");

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        var first = await sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);
        var second = await sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);

        first.RequestId.Should().Be(second.RequestId);
        first.PaymentIntentId.Should().Be(second.PaymentIntentId);
        await stripe.Received(1).CreatePaymentIntentAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        db.CompanyReviewRequests.Count().Should().Be(1);
    }

    [Fact]
    public async Task Rejects_when_scholarship_is_not_open()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = CompanyReviewRequestTestFixtures
            .SeedParticipants(db);
        scholarship.Status = ScholarshipStatus.Closed;
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartCompanyReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*not currently open*");
    }
}
