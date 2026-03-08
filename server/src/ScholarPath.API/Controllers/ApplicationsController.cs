using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Applications.Commands.TrackApplication;
using ScholarPath.Application.Applications.DTOs;
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
}
