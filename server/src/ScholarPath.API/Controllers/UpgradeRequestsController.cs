using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgradeRequest;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Student-facing upgrade-request submission (FR-ONB-07). The Admin queue and
/// the approve / reject flow live under <c>/api/admin/upgrade-queue</c> in
/// <see cref="AdminController"/>; this controller only feeds it.
/// </summary>
[ApiController]
[Route("api/upgrade-requests")]
[Authorize]
[Produces("application/json")]
public sealed class UpgradeRequestsController(IMediator mediator) : ControllerBase
{
    /// <summary>Submit a Consultant upgrade request as an active Student.</summary>
    [HttpPost("consultant")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(SubmitUpgradeResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitConsultantUpgrade(
        [FromBody] SubmitConsultantUpgradeRequestBody body,
        CancellationToken ct)
    {
        var id = await mediator.Send(new SubmitConsultantUpgradeRequestCommand(
            body.Biography,
            body.ProfessionalTitle,
            body.HighestDegree,
            body.FieldOfExpertise,
            body.YearsOfExperience,
            body.SessionFeeUsd,
            body.SessionDurationMinutes,
            body.ExpertiseTags,
            body.Languages,
            body.Country,
            body.Timezone,
            body.LinkedInUrl,
            body.PortfolioUrl), ct).ConfigureAwait(false);
        return StatusCode(StatusCodes.Status201Created, new SubmitUpgradeResponse(id));
    }
}

/// <summary>Wire shape for the Student upgrade-submission endpoint.</summary>
public sealed record SubmitConsultantUpgradeRequestBody(
    string Biography,
    string ProfessionalTitle,
    string HighestDegree,
    string FieldOfExpertise,
    int? YearsOfExperience,
    decimal? SessionFeeUsd,
    int? SessionDurationMinutes,
    string[]? ExpertiseTags,
    string[]? Languages,
    string Country,
    string Timezone,
    string? LinkedInUrl,
    string? PortfolioUrl);

public sealed record SubmitUpgradeResponse(Guid RequestId);
