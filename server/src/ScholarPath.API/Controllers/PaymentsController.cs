using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Payments.Commands.CreateConnectAccount;
using ScholarPath.Application.Payments.Commands.CreatePaymentIntent;
using ScholarPath.Application.Payments.Commands.CapturePaymentIntent;
using ScholarPath.Application.Payments.Commands.RefundPayment;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Payments.Queries.GetMyPayouts;
using ScholarPath.Application.Payments.Queries.GetPayment;
using ScholarPath.Application.Payments.Queries.GetPayments;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Manages payment intents — create, capture, refund, and Stripe Connect onboarding.
/// </summary>
[ApiController]
[Route("api/payments")]
[Authorize]
public sealed class PaymentsController(IMediator mediator) : ControllerBase
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
    /// Creates or reuses the authenticated payee's Stripe Connect account and
    /// returns a fresh onboarding link. Available to consultants and companies.
    /// </summary>
    [HttpPost("connect/onboard")]
    [Authorize(Roles = "Consultant,ScholarshipProvider")]
    [ProducesResponseType(typeof(CreateConnectAccountResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<CreateConnectAccountResult>> Onboard(
        [FromBody] ConnectOnboardingRequest body,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new CreateConnectAccountCommand(body.ReturnUrl, body.RefreshUrl), ct);

        return Ok(result);
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

    // ── GET /api/payments ─────────────────────────────────────────────────────

    /// <summary>
    /// Lists payments newest-first. Admins see every payment; everyone else sees
    /// only payments they are a party to. Supports status/type filters + paging.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PaymentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PaymentDto>>> GetPayments(
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentType? type,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetPaymentsQuery(status, type, page, pageSize), ct);
        return Ok(result);
    }

    // ── GET /api/payments/payouts ─────────────────────────────────────────────

    /// <summary>
    /// Returns the authenticated payee's own payouts (consultants and companies).
    /// </summary>
    [HttpGet("payouts")]
    [Authorize(Roles = "Consultant,ScholarshipProvider")]
    [ProducesResponseType(typeof(IReadOnlyList<PayoutDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayoutDto>>> GetMyPayouts(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyPayoutsQuery(), ct);
        return Ok(result);
    }
}

// ─── Request / Response DTOs (controller-level) ───────────────────────────────

public sealed record RefundPaymentRequest(
    long? AmountCents,
    string? Reason);

public sealed record ConnectOnboardingRequest(
    string RefreshUrl,
    string ReturnUrl);
