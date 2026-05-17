using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ProfitShare;
using ScholarPath.Application.ProfitShare.Commands.SetProfitShareConfig;
using ScholarPath.Application.ProfitShare.Queries.GetActiveProfitShareConfigs;
using ScholarPath.Application.ProfitShare.Queries.GetProfitShareAnalytics;
using ScholarPath.Application.ProfitShare.Queries.GetProfitShareHistory;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers;

/// <summary>Admin profit-share configuration and analytics (PB-014).</summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/profit-share")]
[Produces("application/json")]
public sealed class ProfitShareController(IMediator mediator) : ControllerBase
{
    /// <summary>The currently-active rate for each payment type.</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfitShareConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        var result = await mediator.Send(new GetActiveProfitShareConfigsQuery(), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Full effective-dated rate history, optionally filtered by payment type.</summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(IReadOnlyList<ProfitShareConfigDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHistory(
        [FromQuery] PaymentType? paymentType,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProfitShareHistoryQuery(paymentType), ct).ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Sets a new active rate for a payment type (closes the previous one).</summary>
    [HttpPut("{paymentType}")]
    [ProducesResponseType(typeof(ProfitShareConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SetConfig(
        PaymentType paymentType,
        [FromBody] SetProfitShareConfigBody body,
        CancellationToken ct)
    {
        var result = await mediator.Send(
            new SetProfitShareConfigCommand(paymentType, body.Percentage, body.Notes), ct)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Profit-share totals aggregated per month over a date window.</summary>
    [HttpGet("analytics")]
    [ProducesResponseType(typeof(ProfitShareAnalyticsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAnalytics(
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GetProfitShareAnalyticsQuery(from, to), ct).ConfigureAwait(false);
        return Ok(result);
    }
}

public sealed record SetProfitShareConfigBody(decimal Percentage, string? Notes);
