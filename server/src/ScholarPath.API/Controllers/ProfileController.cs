using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.Commands.UploadProfilePhoto;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Application.Profile.Queries.GetProfile;

namespace ScholarPath.API.Controllers;

/// <summary>Profile and account management (PB-002). All routes require authentication.</summary>
[ApiController]
[Route("api/profiles")]
[Authorize]
[Produces("application/json")]
public sealed class ProfileController(IMediator mediator) : ControllerBase
{
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile(CancellationToken ct)
        => Ok(await mediator.Send(new GetProfileQuery(), ct));

    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile(
        [FromBody] UpdateProfileRequestDto request, CancellationToken ct)
        => Ok(await mediator.Send(new UpdateProfileCommand(request), ct));

    [HttpPost("me/photo")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadPhoto(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        await using var stream = file.OpenReadStream();
        var url = await mediator.Send(new UploadProfilePhotoCommand(
            stream, file.FileName, file.ContentType, file.Length), ct);
        return Ok(new { url });
    }
}
