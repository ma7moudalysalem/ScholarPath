using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Profile.Queries.GetProfile;
using ScholarPath.Application.Profile.Commands.UpdateProfile;
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
}
