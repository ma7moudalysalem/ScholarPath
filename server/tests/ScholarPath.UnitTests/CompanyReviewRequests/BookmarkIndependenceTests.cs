using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.CompanyReviewRequests.Commands.Accept;
using ScholarPath.Application.CompanyReviewRequests.Commands.Cancel;
using ScholarPath.Application.CompanyReviewRequests.Commands.Complete;
using ScholarPath.Application.CompanyReviewRequests.Commands.ConfirmHold;
using ScholarPath.Application.CompanyReviewRequests.Commands.Expire;
using ScholarPath.Application.CompanyReviewRequests.Commands.Reject;
using ScholarPath.Application.CompanyReviewRequests.Commands.Start;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviewRequests;

/// <summary>
/// Regression guard for the bookmark/CompanyReviewRequest independence rule:
/// bookmark state must change ONLY through the explicit BookmarkToggleCommand,
/// never as a side-effect of starting, confirming, accepting, rejecting,
/// cancelling, completing, or expiring a paid review request.
///
/// Each test seeds a pre-existing <see cref="SavedScholarship"/> row, drives
/// the handler under test, and asserts that the bookmark row still exists,
/// untouched. The same goes the other way: each new handler must never
/// insert a bookmark row either.
/// </summary>
public class BookmarkIndependenceTests
{
    private static IStripeService MakeStripeOk(string captureStatus = "succeeded")
    {
        var stripe = Substitute.For<IStripeService>();
        stripe.CreatePaymentIntentAsync(
                Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IDictionary<string, string>>(), Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new StripePaymentIntentResult(
                $"pi_{Guid.NewGuid():N}", "requires_payment_method", "cs_test", null));
        stripe.CapturePaymentIntentAsync(
                Arg.Any<string>(), Arg.Any<long?>(),
                Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(ci => new StripePaymentIntentResult(
                (string)ci[0], captureStatus, null, "ch_test"));
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

    private static SavedScholarship SeedBookmark(
        Infrastructure.Persistence.ApplicationDbContext db,
        Guid userId, Guid scholarshipId)
    {
        var bookmark = new SavedScholarship
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ScholarshipId = scholarshipId,
            SavedAt = DateTimeOffset.UtcNow.AddDays(-1),
            Note = "Saved before Apply Now",
        };
        db.SavedScholarships.Add(bookmark);
        db.SaveChanges();
        return bookmark;
    }

    private static void AssertBookmarkUnchanged(
        Infrastructure.Persistence.ApplicationDbContext db, Guid bookmarkId)
    {
        var row = db.SavedScholarships.SingleOrDefault(s => s.Id == bookmarkId);
        row.Should().NotBeNull(
            "the bookmark must not be deleted by any CompanyReviewRequest state transition");
        row!.Note.Should().Be("Saved before Apply Now",
            "no handler should mutate bookmark fields");
    }

    [Fact]
    public async Task Start_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (scholarship, student, _) = CompanyReviewRequestTestFixtures.SeedParticipants(db);
        var bookmark = SeedBookmark(db, student.Id, scholarship.Id);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(student.Id);

        var sut = new StartCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser,
            NullLogger<StartCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new StartCompanyReviewRequestCommand(scholarship.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
        db.SavedScholarships.Count().Should().Be(1,
            "Start must not insert a new bookmark either");
    }

    [Fact]
    public async Task ConfirmHold_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Submitted);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new ConfirmCompanyReviewRequestHoldCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ConfirmCompanyReviewRequestHoldCommandHandler>.Instance);

        await sut.Handle(new ConfirmCompanyReviewRequestHoldCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Accept_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new AcceptCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<AcceptCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new AcceptCompanyReviewRequestCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Reject_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new RejectCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<RejectCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new RejectCompanyReviewRequestCommand(request.Id, "Not a fit"), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Cancel_from_pending_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new CancelCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new CancelCompanyReviewRequestCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Cancel_from_under_review_with_50pct_refund_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.UnderReview, amountCents: 10_000);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.StudentId);

        var sut = new CancelCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CancelCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new CancelCompanyReviewRequestCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Complete_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.UnderReview);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(request.CompanyId);

        var sut = new CompleteCompanyReviewRequestCommandHandler(
            db, currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<CompleteCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new CompleteCompanyReviewRequestCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }

    [Fact]
    public async Task Expire_does_not_touch_bookmarks()
    {
        using var db = CompanyReviewRequestTestFixtures.CreateDb();
        var (request, _) = CompanyReviewRequestTestFixtures
            .SeedRequestWithPayment(db, CompanyReviewRequestStatus.Pending);
        var bookmark = SeedBookmark(db, request.StudentId, request.ScholarshipId);

        var currentUser = Substitute.For<ICurrentUserService>();
        currentUser.UserId.Returns(Guid.NewGuid());
        currentUser.IsInRole("Admin").Returns(true);

        var sut = new ExpireCompanyReviewRequestCommandHandler(
            db, MakeStripeOk(), currentUser, Substitute.For<INotificationDispatcher>(),
            NullLogger<ExpireCompanyReviewRequestCommandHandler>.Instance);

        await sut.Handle(new ExpireCompanyReviewRequestCommand(request.Id), default);

        AssertBookmarkUnchanged(db, bookmark.Id);
    }
}
