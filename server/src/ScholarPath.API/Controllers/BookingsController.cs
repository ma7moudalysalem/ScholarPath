using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BookingsController : ControllerBase
{
    private readonly ISender _sender;

    public BookingsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPatch("me/availability")]
    public async Task<IActionResult> UpdateMyAvailability(
        [FromBody] UpdateAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        await _sender.Send(command, cancellationToken);
        return NoContent();
    }
}
