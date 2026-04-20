using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Profile.Queries.GetProfile;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
using ScholarPath.Application.Profile.Commands.UploadProfileImage;
using ScholarPath.Application.Profile.DTOs;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/profile")]
[Authorize]
public class ProfileController : BaseController
{
    [HttpGet]
    public async Task<ActionResult<UserProfileDto>> GetProfile(CancellationToken cancellationToken)
    {
        var query = new GetProfileQuery();
        var result = await Mediator.Send(query, cancellationToken);
        return OkResult(result);
    }

    [HttpPut]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromBody] UpdateProfileCommand command, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return OkResult(result);
    }

    [HttpPost("image")]
    public async Task<ActionResult<object>> UploadProfileImage(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequestResult(new[] { "errors.validation.fileRequired" });
        }

        var command = new UploadProfileImageCommand(
            file.FileName,
            file.ContentType,
            file.OpenReadStream());

        var imageUrl = await Mediator.Send(command, cancellationToken);
        return OkResult(new { imageUrl });
    }
}
