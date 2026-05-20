using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Streaming;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Events;

namespace ScholarPath.UnitTests.Streaming;

/// <summary>
/// T-013 — Verifies that each domain-event streaming handler calls
/// <see cref="IEventPublisher.PublishAsync"/> with the correct event-type label
/// and a payload that carries all the expected fields (PB-018).
///
/// The publisher is substituted and the payload is captured via
/// <c>Arg.Do</c>, serialised to JSON, and inspected property-by-property so
/// the test is fully decoupled from the anonymous-type shape.
/// </summary>
public class DomainEventPublishingHandlerTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a substituted <see cref="IEventPublisher"/> that captures every
    /// call and appends it to the returned list as a parsed <see cref="JsonDocument"/>.
    /// </summary>
    private static (IEventPublisher publisher, List<(string EventType, JsonDocument Doc)> captured)
        MakeCapturingPublisher()
    {
        var publisher = Substitute.For<IEventPublisher>();
        var captured  = new List<(string, JsonDocument)>();

        publisher
            .PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var eventType = call.ArgAt<string>(0);
                var payload   = call.ArgAt<object>(1);
                var json      = JsonSerializer.Serialize(payload);
                captured.Add((eventType, JsonDocument.Parse(json)));
                return Task.CompletedTask;
            });

        return (publisher, captured);
    }

    // ── ApplicationSubmittedStreamHandler ─────────────────────────────────────

    [Fact]
    public async Task ApplicationSubmitted_publishes_correct_eventType_and_payload()
    {
        var (publisher, captured) = MakeCapturingPublisher();
        var handler = new ApplicationSubmittedStreamHandler(
            publisher, NullLogger<ApplicationSubmittedStreamHandler>.Instance);

        var applicationId  = Guid.NewGuid();
        var studentId      = Guid.NewGuid();
        var scholarshipId  = Guid.NewGuid();
        var evt            = new ApplicationSubmittedEvent(applicationId, studentId, scholarshipId);

        await handler.Handle(evt, CancellationToken.None);

        // exactly one call, with the right event-type label
        await publisher.Received(1)
            .PublishAsync("ApplicationSubmitted", Arg.Any<object>(), Arg.Any<CancellationToken>());

        // payload carries every expected field
        captured.Should().HaveCount(1);
        var root = captured[0].Doc.RootElement;
        root.GetProperty("applicationId").GetGuid().Should().Be(applicationId);
        root.GetProperty("studentId").GetGuid().Should().Be(studentId);
        root.GetProperty("scholarshipId").GetGuid().Should().Be(scholarshipId);
    }

    // ── PaymentCapturedStreamHandler ──────────────────────────────────────────

    [Fact]
    public async Task PaymentCaptured_publishes_correct_eventType_and_payload()
    {
        var (publisher, captured) = MakeCapturingPublisher();
        var handler = new PaymentCapturedStreamHandler(
            publisher, NullLogger<PaymentCapturedStreamHandler>.Instance);

        var paymentId   = Guid.NewGuid();
        var payerUserId = Guid.NewGuid();
        var payeeUserId = Guid.NewGuid();
        var evt = new PaymentCapturedEvent(
            paymentId, PaymentType.ConsultantBooking, 7500, payerUserId, payeeUserId);

        await handler.Handle(evt, CancellationToken.None);

        await publisher.Received(1)
            .PublishAsync("PaymentCaptured", Arg.Any<object>(), Arg.Any<CancellationToken>());

        captured.Should().HaveCount(1);
        var root = captured[0].Doc.RootElement;
        root.GetProperty("paymentId").GetGuid().Should().Be(paymentId);
        root.GetProperty("amountCents").GetInt64().Should().Be(7500);
        root.GetProperty("type").GetString().Should().Be(nameof(PaymentType.ConsultantBooking));
        root.GetProperty("payerUserId").GetGuid().Should().Be(payerUserId);
        root.GetProperty("payeeUserId").GetGuid().Should().Be(payeeUserId);
    }

    [Fact]
    public async Task PaymentCaptured_payload_payeeUserId_is_null_when_no_payee()
    {
        var (publisher, captured) = MakeCapturingPublisher();
        var handler = new PaymentCapturedStreamHandler(
            publisher, NullLogger<PaymentCapturedStreamHandler>.Instance);

        var evt = new PaymentCapturedEvent(
            Guid.NewGuid(), PaymentType.CompanyReview, 1000, Guid.NewGuid(), PayeeUserId: null);

        await handler.Handle(evt, CancellationToken.None);

        captured.Should().HaveCount(1);
        var root = captured[0].Doc.RootElement;
        root.GetProperty("payeeUserId").ValueKind.Should().Be(JsonValueKind.Null);
    }

    // ── BookingCompletedStreamHandler ─────────────────────────────────────────

    [Fact]
    public async Task BookingCompleted_publishes_correct_eventType_and_payload()
    {
        var (publisher, captured) = MakeCapturingPublisher();
        var handler = new BookingCompletedStreamHandler(
            publisher, NullLogger<BookingCompletedStreamHandler>.Instance);

        var bookingId    = Guid.NewGuid();
        var studentId    = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var evt          = new BookingCompletedEvent(bookingId, studentId, consultantId);

        await handler.Handle(evt, CancellationToken.None);

        await publisher.Received(1)
            .PublishAsync("BookingCompleted", Arg.Any<object>(), Arg.Any<CancellationToken>());

        captured.Should().HaveCount(1);
        var root = captured[0].Doc.RootElement;
        root.GetProperty("bookingId").GetGuid().Should().Be(bookingId);
        root.GetProperty("studentId").GetGuid().Should().Be(studentId);
        root.GetProperty("consultantId").GetGuid().Should().Be(consultantId);
    }

    // ── no cross-contamination ────────────────────────────────────────────────

    [Fact]
    public async Task Each_handler_only_calls_publisher_once_per_event()
    {
        var (publisher, _) = MakeCapturingPublisher();

        var appHandler = new ApplicationSubmittedStreamHandler(
            publisher, NullLogger<ApplicationSubmittedStreamHandler>.Instance);
        var payHandler = new PaymentCapturedStreamHandler(
            publisher, NullLogger<PaymentCapturedStreamHandler>.Instance);
        var bkgHandler = new BookingCompletedStreamHandler(
            publisher, NullLogger<BookingCompletedStreamHandler>.Instance);

        await appHandler.Handle(
            new ApplicationSubmittedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        await payHandler.Handle(
            new PaymentCapturedEvent(Guid.NewGuid(), PaymentType.ConsultantBooking, 1, Guid.NewGuid(), null),
            CancellationToken.None);

        await bkgHandler.Handle(
            new BookingCompletedEvent(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        // three events → exactly three PublishAsync calls, each with a distinct event type
        await publisher.Received(3)
            .PublishAsync(Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
        await publisher.Received(1)
            .PublishAsync("ApplicationSubmitted", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await publisher.Received(1)
            .PublishAsync("PaymentCaptured", Arg.Any<object>(), Arg.Any<CancellationToken>());
        await publisher.Received(1)
            .PublishAsync("BookingCompleted", Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
