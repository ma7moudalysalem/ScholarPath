using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
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

    [HttpPost("/api/consultants/{id:guid}/book")]
    public async Task<IActionResult> RequestBooking(
        Guid id,
        [FromBody] RequestBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.ConsultantId)
        {
            return BadRequest("Route consultant id does not match body consultant id.");
        }

        var bookingId = await _sender.Send(command, cancellationToken);
        return Ok(new { bookingId });
    }

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> AcceptBooking(
        Guid id,
        [FromBody] AcceptBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.BookingId)
        {
            return BadRequest("Route booking id does not match body booking id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> RejectBooking(
        Guid id,
        [FromBody] RejectBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.BookingId)
        {
            return BadRequest("Route booking id does not match body booking id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
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
