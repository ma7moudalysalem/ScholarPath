using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Analytics.Queries.GetPowerBiEmbedToken;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Power BI analytics embed-token endpoint (PB-015 T-014).
///
/// Role access rules (enforced in the query handler):
///   Admin / SuperAdmin → any report type
///   Consultant         → ConsultantSelfAnalytics only
///   Student            → StudentSelfAnalytics only
/// </summary>
[ApiController]
[Authorize]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns a short-lived Power BI embed token for the requested report,
    /// scoped to the caller's identity via RLS.  Returns 503 when the Power BI
    /// workspace has not been provisioned yet.
    /// </summary>
    /// <param name="reportType">
    /// One of: ExecutiveDashboard | StudentSuccessDashboard | FinancialDashboard |
    /// ConsultantSelfAnalytics | StudentSelfAnalytics
    /// </param>
    [HttpGet("embed-token")]
    [ProducesResponseType(typeof(EmbedTokenDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetEmbedToken(
        [FromQuery] string reportType,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetPowerBiEmbedTokenQuery(reportType), ct)
            .ConfigureAwait(false);

        if (!result.IsConfigured)
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new { message = "Power BI workspace is not provisioned yet." });

        return Ok(result);
    }
}
