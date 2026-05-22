using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.CompanyReviews.Commands.CaptureCompanyReviewPayment;
using ScholarPath.Application.CompanyReviews.Commands.RejectCompanyReviewPayment;
using ScholarPath.Application.CompanyReviews.EventHandlers;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;
using Xunit;

namespace ScholarPath.UnitTests.CompanyReviews;

/// <summary>
/// PB-005 v1: only an Accepted application captures the held review fee;
/// a Rejected application cancels the hold. Intermediate states (UnderReview
/// etc.) and external-tracking states must not touch the payment.
/// </summary>
public sealed class ApplicationStatusChangedEventHandlerTests
{
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly ApplicationStatusChangedEventHandler _handler;

    public ApplicationStatusChangedEventHandlerTests()
    {
        _handler = new ApplicationStatusChangedEventHandler(
            _sender,
            NullLogger<ApplicationStatusChangedEventHandler>.Instance);
    }

    [Fact]
    public async Task Accepted_status_triggers_CaptureCompanyReviewPayment()
    {
        var appId = Guid.NewGuid();
        var evt = new ApplicationStatusChangedEvent(
            appId, Guid.NewGuid(), Guid.NewGuid(),
            ApplicationStatus.UnderReview, ApplicationStatus.Accepted);

        await _handler.Handle(evt, default);

        await _sender.Received(1).Send(
            Arg.Is<CaptureCompanyReviewPaymentCommand>(c => c.ApplicationId == appId),
            Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(
            Arg.Any<RejectCompanyReviewPaymentCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejected_status_triggers_RejectCompanyReviewPayment_not_capture()
    {
        var appId = Guid.NewGuid();
        var evt = new ApplicationStatusChangedEvent(
            appId, Guid.NewGuid(), Guid.NewGuid(),
            ApplicationStatus.UnderReview, ApplicationStatus.Rejected);

        await _handler.Handle(evt, default);

        await _sender.Received(1).Send(
            Arg.Is<RejectCompanyReviewPaymentCommand>(c => c.ApplicationId == appId),
            Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(
            Arg.Any<CaptureCompanyReviewPaymentCommand>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ApplicationStatus.UnderReview)]
    [InlineData(ApplicationStatus.Shortlisted)]
    [InlineData(ApplicationStatus.Pending)]
    [InlineData(ApplicationStatus.Withdrawn)]
    [InlineData(ApplicationStatus.Intending)]
    public async Task Non_terminal_status_does_not_touch_payment(ApplicationStatus status)
    {
        var evt = new ApplicationStatusChangedEvent(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            ApplicationStatus.Draft, status);

        await _handler.Handle(evt, default);

        await _sender.DidNotReceive().Send(
            Arg.Any<CaptureCompanyReviewPaymentCommand>(),
            Arg.Any<CancellationToken>());
        await _sender.DidNotReceive().Send(
            Arg.Any<RejectCompanyReviewPaymentCommand>(),
            Arg.Any<CancellationToken>());
    }
}
