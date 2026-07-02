using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Exceptions;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Start;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.ScholarshipProviderReviewRequests;

public class StartScholarshipProviderReviewRequestCommandHandlerTests
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
        var v = new StartScholarshipProviderReviewRequestCommandValidator();
        v.Validate(new StartScholarshipProviderReviewRequestCommand(Guid.Empty))
            .IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task Creates_request_and_manual_capture_intent_for_open_scholarship_with_fee()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 150m);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = MakeStripeReturningClientSecret("pi_xyz");

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        result.AmountCents.Should().Be(15_000);
        result.Currency.Should().Be("USD");

        // Stripe was called with manual capture (PB-005: hold-then-capture-on-accept).
        await stripe.Received(1).CreatePaymentIntentAsync(
            15_000, "usd", "manual",
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());

        var entity = db.ScholarshipProviderReviewRequests.Single();
        entity.Status.Should().Be(ScholarshipProviderReviewRequestStatus.Submitted);
        entity.StudentId.Should().Be(student.Id);
        entity.ScholarshipProviderId.Should().Be(scholarship.OwnerScholarshipProviderId!.Value);
        entity.ReviewFeeUsdSnapshot.Should().Be(150m);
        entity.PendingExpiresAt.Should().NotBeNull();

        var payment = db.Payments.Single();
        payment.Type.Should().Be(PaymentType.ScholarshipProviderReview);
        payment.Status.Should().Be(PaymentStatus.Pending);
        payment.AmountCents.Should().Be(15_000);
        // Default 10/90 split locked at submission (re-locked on capture).
        payment.ProfitShareAmountCents.Should().Be(1_500);
        payment.PayeeAmountCents.Should().Be(13_500);
    }

    [Fact]
    public async Task Rejects_when_scholarship_has_no_review_fee()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: null);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*Review Service Fee*");
    }

    [Fact]
    public async Task Rejects_when_company_tries_to_apply_to_its_own_scholarship()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, _, company) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(company.Id);

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*own scholarship*");
    }

    [Fact]
    public async Task Returns_existing_submitted_request_on_double_click()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = MakeStripeReturningClientSecret("pi_only_once");

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var first = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);
        var second = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        first.RequestId.Should().Be(second.RequestId);
        first.PaymentIntentId.Should().Be(second.PaymentIntentId);
        await stripe.Received(1).CreatePaymentIntentAsync(
            Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IDictionary<string, string>>(),
            Arg.Any<string>(), Arg.Any<CancellationToken>());
        db.ScholarshipProviderReviewRequests.Count().Should().Be(1);
    }

    [Fact]
    public async Task Free_scholarship_skips_Stripe_and_lands_directly_in_Pending()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 0m);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);
        var stripe = Substitute.For<IStripeService>();

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, stripe, currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var result = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        result.IsFree.Should().BeTrue();
        result.PaymentId.Should().BeNull();
        result.ClientSecret.Should().BeNull();
        result.PaymentIntentId.Should().BeNull();
        result.AmountCents.Should().Be(0);

        // Stripe must NOT be called for free requests — a 0-amount intent
        // would be rejected by Stripe with an api_error.
        await stripe.DidNotReceiveWithAnyArgs().CreatePaymentIntentAsync(
            default, default!, default!, default!, default!, default);

        var entity = db.ScholarshipProviderReviewRequests.Single();
        entity.Status.Should().Be(ScholarshipProviderReviewRequestStatus.Pending);
        entity.PaymentId.Should().BeNull();
        entity.ReviewFeeUsdSnapshot.Should().Be(0m);

        db.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Free_path_is_idempotent_on_double_click()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db, reviewFeeUsd: 0m);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, Substitute.For<IStripeService>(), currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var first = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);
        var second = await sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        first.RequestId.Should().Be(second.RequestId);
        first.IsFree.Should().BeTrue();
        second.IsFree.Should().BeTrue();
        db.ScholarshipProviderReviewRequests.Count().Should().Be(1);
    }

    [Fact]
    public async Task Rejects_when_scholarship_is_not_open()
    {
        using var db = ScholarshipProviderReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = ScholarshipProviderReviewRequestTestFixtures
            .SeedParticipants(db);
        scholarship.Status = ScholarshipStatus.Closed;
        db.SaveChanges();

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartScholarshipProviderReviewRequestCommandHandler(
            db, MakeStripeReturningClientSecret(), currentUser,
            NullLogger<StartScholarshipProviderReviewRequestCommandHandler>.Instance);

        var act = () => sut.Handle(
            new StartScholarshipProviderReviewRequestCommand(scholarship.Id), default);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*not currently open*");
    }
}
