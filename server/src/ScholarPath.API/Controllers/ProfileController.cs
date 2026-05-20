using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Profile.Commands.ChangePassword;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.Commands.UploadProfilePhoto;
using ScholarPath.Application.Profile.DTOs;
using ScholarPath.Application.Profile.Queries.GetProfile;
using ScholarPath.Application.Profile.Queries.GetProfilePhoto;

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

    /// <summary>
    /// Changes the signed-in user's password.
    /// Revokes all existing refresh tokens on success so other sessions must
    /// re-authenticate (PB-002 T-005).
    /// </summary>
    [HttpPost("me/change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest body, CancellationToken ct)
    {
        await mediator.Send(
            new ChangePasswordCommand(body.CurrentPassword, body.NewPassword), ct);
        return NoContent();
    }

    /// <summary>
    /// Streams a user's profile photo. Anonymous-accessible — profile photos are
    /// shown on the public consultant-browse pages. Returns 404 when the user has
    /// no photo.
    /// </summary>
    [HttpGet("{userId:guid}/photo")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status302Found)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPhoto(Guid userId, CancellationToken ct)
    {
        var photo = await mediator.Send(new GetProfilePhotoQuery(userId), ct);
        if (photo is null)
            return NotFound();

        // Profile photos rarely change and are addressed by a stable per-user
        // URL — let browsers cache them (the client cache-busts after an upload).
        Response.Headers.CacheControl = "public, max-age=86400";

        if (photo.RedirectUrl is not null)
            return Redirect(photo.RedirectUrl);

        return File(photo.Content!, photo.ContentType ?? "application/octet-stream");
    }
}

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
