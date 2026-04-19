using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Audit.Commands.CancelDataDelete;
using ScholarPath.Application.Audit.Commands.RequestDataDelete;
using ScholarPath.Application.Audit.Commands.RequestDataExport;
using ScholarPath.Application.Audit.DTOs;
using ScholarPath.Application.Audit.Queries.GetMyDataRequests;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users/me")]
[Produces("application/json")]
public sealed class DataPrivacyController(IMediator mediator) : ControllerBase
{
    [HttpGet("data-requests")]
    [ProducesResponseType(typeof(IReadOnlyList<DataRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyDataRequestsQuery(), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("data-export")]
    [ProducesResponseType(typeof(DataRequestDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestExport(CancellationToken ct)
    {
        var result = await mediator.Send(new RequestDataExportCommand(), ct).ConfigureAwait(false);
        return Accepted(result);
    }

    [HttpPost("data-delete")]
    [ProducesResponseType(typeof(DataRequestDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RequestDelete([FromBody] RequestDataDeleteCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return Accepted(result);
    }

    [HttpPost("data-delete/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelDelete(CancellationToken ct)
    {
        await mediator.Send(new CancelDataDeleteCommand(), ct).ConfigureAwait(false);
        return NoContent();
    }
}
