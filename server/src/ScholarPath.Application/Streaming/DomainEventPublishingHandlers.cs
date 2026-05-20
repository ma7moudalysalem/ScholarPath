using MediatR;
using Microsoft.Extensions.Logging;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Domain.Events;

namespace ScholarPath.Application.Streaming;

/// <summary>
/// MediatR <see cref="INotificationHandler{TNotification}"/> implementations that
/// forward the three high-value domain events to the Azure Event Hubs streaming
/// pipeline (PB-018 T-002).
///
/// Registered automatically by MediatR's assembly scan. Each handler fires
/// asynchronously after the business transaction commits — they share the same
/// DI scope so the producer client inherits the correct options.
///
/// The handlers are intentionally thin: build a minimal envelope and delegate to
/// <see cref="IEventPublisher"/>. All error handling (retry / swallow) lives in
/// the publisher so the handler contracts stay simple.
/// </summary>

// ── ApplicationSubmitted ──────────────────────────────────────────────────────

public sealed class ApplicationSubmittedStreamHandler(
    IEventPublisher publisher,
    ILogger<ApplicationSubmittedStreamHandler> logger)
    : INotificationHandler<ApplicationSubmittedEvent>
{
    public async Task Handle(ApplicationSubmittedEvent n, CancellationToken ct)
    {
        logger.LogDebug(
            "Streaming ApplicationSubmitted {ApplicationId} for student {StudentId}.",
            n.ApplicationId, n.StudentId);

        await publisher.PublishAsync(
            "ApplicationSubmitted",
            new
            {
                applicationId = n.ApplicationId,
                studentId     = n.StudentId,
                scholarshipId = n.ScholarshipId,
            },
            ct).ConfigureAwait(false);
    }
}

// ── PaymentCaptured ───────────────────────────────────────────────────────────

public sealed class PaymentCapturedStreamHandler(
    IEventPublisher publisher,
    ILogger<PaymentCapturedStreamHandler> logger)
    : INotificationHandler<PaymentCapturedEvent>
{
    public async Task Handle(PaymentCapturedEvent n, CancellationToken ct)
    {
        logger.LogDebug(
            "Streaming PaymentCaptured {PaymentId} amount={AmountCents} type={Type}.",
            n.PaymentId, n.AmountCents, n.Type);

        await publisher.PublishAsync(
            "PaymentCaptured",
            new
            {
                paymentId   = n.PaymentId,
                type        = n.Type.ToString(),
                amountCents = n.AmountCents,
                payerUserId = n.PayerUserId,
                payeeUserId = n.PayeeUserId,
            },
            ct).ConfigureAwait(false);
    }
}

// ── BookingCompleted ──────────────────────────────────────────────────────────

public sealed class BookingCompletedStreamHandler(
    IEventPublisher publisher,
    ILogger<BookingCompletedStreamHandler> logger)
    : INotificationHandler<BookingCompletedEvent>
{
    public async Task Handle(BookingCompletedEvent n, CancellationToken ct)
    {
        logger.LogDebug(
            "Streaming BookingCompleted {BookingId} student={StudentId} consultant={ConsultantId}.",
            n.BookingId, n.StudentId, n.ConsultantId);

        await publisher.PublishAsync(
            "BookingCompleted",
            new
            {
                bookingId    = n.BookingId,
                studentId    = n.StudentId,
                consultantId = n.ConsultantId,
            },
            ct).ConfigureAwait(false);
    }
}
