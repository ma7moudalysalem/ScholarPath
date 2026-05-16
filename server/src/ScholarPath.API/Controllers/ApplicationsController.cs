using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.ReviewApplication;
using ScholarPath.Application.Applications.Commands.StartApplication;
using ScholarPath.Application.Applications.Commands.SubmitApplication;
using ScholarPath.Application.Applications.Commands.UpdateExternalStatus;
using ScholarPath.Application.Applications.Commands.WithdrawApplication;
using ScholarPath.Application.Applications.Queries.GetCompanyApplicationDetails;
using ScholarPath.Application.Applications.Queries.GetCompanyApplications;
using ScholarPath.Application.Applications.Queries.GetMyApplications;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Scholarship applications — student write-side (PB-004) and the company-side
/// review flow (PB-005). Authorization is applied per endpoint because the
/// controller mixes Student and Company actions.
/// </summary>
[ApiController]
[Route("api/applications")]
public sealed class ApplicationsController(IMediator mediator) : ControllerBase
{
    // ─── Student write-side (PB-004) ─────────────────────────────────────────

    /// <summary>Creates a new Draft application for the authenticated student.</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Start(StartApplicationCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Submits a Draft application, transitioning it to Pending.</summary>
    [HttpPut("{id:guid}/submit")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Submit(Guid id, CancellationToken ct)
    {
        await mediator.Send(new SubmitApplicationCommand(id), ct);
        return NoContent();
    }

    /// <summary>Withdraws an active application (triggers refund per policy).</summary>
    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Withdraw(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new WithdrawApplicationCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Retrieves a single application by ID. Read-side pending.</summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public ActionResult GetById(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            "Read-side for a single application is not yet implemented.");

    // ─── Student read-side / external tracking (PB-005 slice) ────────────────

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyApplications(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyApplicationsQuery(), ct);
        return Ok(result);
    }

    [HttpPatch("{id:guid}/external-status")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateExternalStatus(
        Guid id, [FromBody] UpdateExternalStatusRequest request, CancellationToken ct)
    {
        var ok = await mediator.Send(new UpdateExternalStatusCommand(id, request.Status), ct);
        return ok ? Ok() : BadRequest();
    }

    // ─── Company review-side (PB-005) ────────────────────────────────────────

    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplications(
        [FromQuery] Guid? scholarshipId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(
            new GetCompanyApplicationsQuery(scholarshipId, page, pageSize), ct);
        return Ok(result);
    }

    [HttpGet("company/{id:guid}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplicationDetails(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new GetCompanyApplicationDetailsQuery(id), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> ReviewApplication(
        Guid id, [FromBody] ReviewApplicationRequest request, CancellationToken ct)
    {
        var ok = await mediator.Send(
            new ReviewApplicationCommand(id, request.Status, request.DecisionReason), ct);
        return ok ? Ok() : BadRequest();
    }
}

public record UpdateExternalStatusRequest(ScholarPath.Domain.Enums.ApplicationStatus Status);
public record ReviewApplicationRequest(
    ScholarPath.Domain.Enums.ApplicationStatus Status, string? DecisionReason);
