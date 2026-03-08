using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.DeleteApplication;
using ScholarPath.Application.Applications.Commands.TrackApplication;
using ScholarPath.Application.Applications.Commands.UpdateApplicationChecklist;
using ScholarPath.Application.Applications.Commands.UpdateApplicationNotes;
using ScholarPath.Application.Applications.Commands.UpdateApplicationStatus;
using ScholarPath.Application.Applications.Commands.UpdateReminders;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Applications.Queries.GetApplications;
using ScholarPath.Domain.Entities;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/applications")]
[Authorize]
public class ApplicationsController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ApplicationsController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpPost("track")]
    public async Task<IActionResult> Track(
        [FromBody] TrackApplicationRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new TrackApplicationCommand(
            request.ScholarshipId, user.Id, request.Status, request.Notes
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok(result.Value!);
    }

    [HttpGet]
    public async Task<IActionResult> GetApplications(
        [FromQuery] GetApplicationsRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new GetApplicationsQuery(
            user.Id, request.Status, request.SortBy, request.Page, request.PageSize
        ), ct);

        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new UpdateApplicationStatusCommand(
            id, user.Id, request.Status
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok(result.Value!);
    }

    [HttpPut("{id:guid}/notes")]
    public async Task<IActionResult> UpdateNotes(
        Guid id,
        [FromBody] UpdateNotesRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new UpdateApplicationNotesCommand(
            id, user.Id, request.Notes
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok(result.Value!);
    }

    [HttpPut("{id:guid}/checklist")]
    public async Task<IActionResult> UpdateChecklist(
        Guid id,
        [FromBody] UpdateChecklistRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new UpdateApplicationChecklistCommand(
            id, user.Id, request.Items
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok(result.Value!);
    }

    [HttpPut("{id:guid}/reminders")]
    public async Task<IActionResult> UpdateReminders(
        Guid id,
        [FromBody] UpdateRemindersRequest request,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new UpdateRemindersCommand(
            id, user.Id, request.Presets, request.Channels, request.Paused
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok(result.Value!);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken ct)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return UnauthorizedResult("errors.auth.unauthorized");

        var result = await Mediator.Send(new DeleteApplicationCommand(
            id, user.Id
        ), ct);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok();
    }
}
