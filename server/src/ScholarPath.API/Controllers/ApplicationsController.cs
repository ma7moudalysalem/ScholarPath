using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.ReviewApplication;
using ScholarPath.Application.Applications.Commands.UpdateExternalStatus;
using ScholarPath.Application.Applications.Commands.WithdrawApplication;
using ScholarPath.Application.Applications.Queries.GetCompanyApplicationDetails;
using ScholarPath.Application.Applications.Queries.GetCompanyApplications;
using ScholarPath.Application.Applications.Queries.GetMyApplications;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ApplicationsController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyApplications(CancellationToken ct)
    {
        var query = new GetMyApplicationsQuery();
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("company")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplications([FromQuery] Guid? scholarshipId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var query = new GetCompanyApplicationsQuery(scholarshipId, page, pageSize);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpGet("company/{id:guid}")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> GetCompanyApplicationDetails(Guid id, CancellationToken ct)
    {
        var query = new GetCompanyApplicationDetailsQuery(id);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/review")]
    [Authorize(Roles = "Company")]
    public async Task<IActionResult> ReviewApplication(Guid id, [FromBody] ReviewApplicationRequest request, CancellationToken ct)
    {
        var command = new ReviewApplicationCommand(id, request.Status, request.DecisionReason);
        var result = await mediator.Send(command, ct);
        return result ? Ok() : BadRequest();
    }

    [HttpPost("{id:guid}/withdraw")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> WithdrawApplication(Guid id, CancellationToken ct)
    {
        var command = new WithdrawApplicationCommand(id);
        var result = await mediator.Send(command, ct);
        return result ? Ok() : BadRequest();
    }

    [HttpPatch("{id:guid}/external-status")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdateExternalStatus(Guid id, [FromBody] UpdateExternalStatusRequest request, CancellationToken ct)
    {
        var command = new UpdateExternalStatusCommand(id, request.Status);
        var result = await mediator.Send(command, ct);
        return result ? Ok() : BadRequest();
    }
}

public record UpdateExternalStatusRequest(Domain.Enums.ApplicationStatus Status);
public record ReviewApplicationRequest(Domain.Enums.ApplicationStatus Status, string? DecisionReason);
