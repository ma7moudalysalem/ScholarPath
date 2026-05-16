using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.StartApplication;
using ScholarPath.Application.Applications.Commands.SubmitApplication;
using ScholarPath.Application.Applications.Commands.WithdrawApplication;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Manages student scholarship applications.
/// Write operations: Start, Submit, Withdraw.
/// Read operations will be added in the read-side module (I7).
/// </summary>
[ApiController]
[Route("api/applications")]
[Authorize(Roles = "Student")]
public sealed class ApplicationsController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a new Draft application for the authenticated student.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Start(
        StartApplicationCommand command,
        CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Submits a Draft application, transitioning it to Pending.
    /// </summary>
    [HttpPut("{id:guid}/submit")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Submit(
        Guid id,
        CancellationToken ct)
    {
        await mediator.Send(new SubmitApplicationCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Withdraws an active application.
    /// </summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult> Withdraw(
        Guid id,
        CancellationToken ct)
    {
        await mediator.Send(new WithdrawApplicationCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Retrieves a single application by ID.
    /// Read-side implementation pending (I7).
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public ActionResult GetById(Guid id)
        => StatusCode(StatusCodes.Status501NotImplemented,
            "Read-side for applications is not yet implemented.");
}
