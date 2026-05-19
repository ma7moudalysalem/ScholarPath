using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.FinancialConfig;
using ScholarPath.Application.FinancialConfig.Commands.ActivateFinancialRule;
using ScholarPath.Application.FinancialConfig.Commands.ArchiveFinancialRule;
using ScholarPath.Application.FinancialConfig.Commands.CreateFinancialRule;
using ScholarPath.Application.FinancialConfig.Commands.DeactivateFinancialRule;
using ScholarPath.Application.FinancialConfig.Commands.UpdateFinancialRule;
using ScholarPath.Application.FinancialConfig.Queries.GetFinancialRuleById;
using ScholarPath.Application.FinancialConfig.Queries.GetFinancialRules;
using ScholarPath.Application.FinancialConfig.Queries.PreviewFinancialCalculation;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers;

/// <summary>Admin financial-configuration rules: platform fee + profit-share (FR-163..176).</summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/financial-config")]
[Produces("application/json")]
public sealed class FinancialConfigController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists rules (newest first), optionally filtered by payment type and/or status.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<FinancialConfigRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(
        [FromQuery] PaymentType? paymentType,
        [FromQuery] FinancialRuleStatus? status,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new GetFinancialRulesQuery(paymentType, status), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Returns a single rule by id.</summary>
    [HttpGet("{id:guid}", Name = nameof(GetById))]
    [ProducesResponseType(typeof(FinancialConfigRuleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await mediator
            .Send(new GetFinancialRuleByIdQuery(id), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Simulates how a gross amount would be split under a rule (FR-167/175).</summary>
    [HttpGet("preview")]
    [ProducesResponseType(typeof(FinancialCalculationPreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Preview(
        [FromQuery] long grossAmountCents,
        [FromQuery] PaymentType? paymentType,
        [FromQuery] Guid? ruleId,
        CancellationToken ct)
    {
        var result = await mediator
            .Send(new PreviewFinancialCalculationQuery(grossAmountCents, paymentType, ruleId), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Creates a new rule in Draft state (FR-165).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateFinancialRuleBody body,
        CancellationToken ct)
    {
        var id = await mediator.Send(
            new CreateFinancialRuleCommand(
                body.PaymentType, body.FeeKind, body.FeePercentage, body.FeeAmountCents,
                body.ProfitSharePercentage, body.EffectiveFrom, body.EffectiveTo, body.Notes), ct)
            .ConfigureAwait(false);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Edits a Draft rule (FR-171). Active/Archived rules are immutable.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateFinancialRuleBody body,
        CancellationToken ct)
    {
        await mediator.Send(
            new UpdateFinancialRuleCommand(
                id, body.FeeKind, body.FeePercentage, body.FeeAmountCents,
                body.ProfitSharePercentage, body.EffectiveFrom, body.EffectiveTo, body.Notes), ct)
            .ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Activates a Draft rule, archiving the rule currently in force (FR-170).</summary>
    [HttpPost("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new ActivateFinancialRuleCommand(id), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Takes an Active rule out of service, returning it to Draft.</summary>
    [HttpPost("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeactivateFinancialRuleCommand(id), ct).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Archives a rule — the retire path; rules are never hard-deleted (FR-176).</summary>
    [HttpPost("{id:guid}/archive")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
    {
        await mediator.Send(new ArchiveFinancialRuleCommand(id), ct).ConfigureAwait(false);
        return NoContent();
    }
}

public sealed record CreateFinancialRuleBody(
    PaymentType PaymentType,
    FeeKind FeeKind,
    decimal? FeePercentage,
    long? FeeAmountCents,
    decimal ProfitSharePercentage,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Notes);

public sealed record UpdateFinancialRuleBody(
    FeeKind FeeKind,
    decimal? FeePercentage,
    long? FeeAmountCents,
    decimal ProfitSharePercentage,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo,
    string? Notes);
