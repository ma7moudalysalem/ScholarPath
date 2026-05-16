using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Payments.Commands.CreatePaymentIntent;
using ScholarPath.Application.Payments.Commands.CapturePaymentIntent;
using ScholarPath.Application.Payments.Commands.RefundPayment;
using ScholarPath.Application.Payments.Queries.GetPayment;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Manages payment intents — create, capture, refund, and Stripe Connect onboarding.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController(
    IMediator mediator,
    IStripeService stripeService) : ControllerBase
{
    // ── POST /api/payments/intent ─────────────────────────────────────────────

    /// <summary>
    /// Creates a Stripe PaymentIntent and a matching Payment row.
    /// Returns the ClientSecret for the frontend to confirm payment.
    /// </summary>
    [HttpPost("intent")]
    [ProducesResponseType(typeof(CreatePaymentIntentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CreatePaymentIntentResult>> CreateIntent(
        [FromBody] CreatePaymentIntentCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.PaymentId }, result);
    }

    // ── POST /api/payments/{id}/capture ───────────────────────────────────────

    /// <summary>
    /// Captures a held PaymentIntent (manual capture flow).
    /// Called when a consultant accepts a booking.
    /// </summary>
    [HttpPost("{id:guid}/capture")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Capture(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CapturePaymentIntentCommand(id), ct);

        return result ? NoContent() : NotFound();
    }

    // ── POST /api/payments/{id}/refund ────────────────────────────────────────

    /// <summary>
    /// Refunds a held or captured payment — full or partial.
    /// </summary>
    [HttpPost("{id:guid}/refund")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Refund(
        Guid id,
        [FromBody] RefundPaymentRequest body,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new RefundPaymentCommand(id, body.AmountCents, body.Reason), ct);

        return result ? NoContent() : NotFound();
    }

    // ── POST /api/payments/connect/onboard ────────────────────────────────────

    /// <summary>
    /// Generates a Stripe Connect onboarding link for the authenticated consultant.
    /// </summary>
    [HttpPost("connect/onboard")]
    [Authorize(Roles = "Consultant")]
    [ProducesResponseType(typeof(ConnectOnboardingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConnectOnboardingResult>> Onboard(
        [FromBody] ConnectOnboardingRequest body,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(body.ConnectAccountId))
            return BadRequest("ConnectAccountId is required.");

        var url = await stripeService.CreateConnectOnboardingLinkAsync(
            body.ConnectAccountId,
            body.RefreshUrl,
            body.ReturnUrl,
            ct);

        return Ok(new ConnectOnboardingResult(url));
    }

    // ── GET /api/payments/{id} ────────────────────────────────────────────────

    /// <summary>
    /// Returns a single payment by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PaymentDto>> GetById(
        Guid id,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetPaymentQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }
}

// ─── Request / Response DTOs (controller-level) ───────────────────────────────

public sealed record RefundPaymentRequest(
    long? AmountCents,
    string? Reason);

public sealed record ConnectOnboardingRequest(
    string ConnectAccountId,
    string RefreshUrl,
    string ReturnUrl);

public sealed record ConnectOnboardingResult(string OnboardingUrl);
