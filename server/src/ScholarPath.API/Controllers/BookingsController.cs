using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;
using ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;
using ScholarPath.Application.ConsultantBookings.Commands.HideConsultantReview;
using ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;
using ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;
using ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;
using ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Application.ConsultantBookings.Queries.GetBookingById;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantBookings;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyBookings;

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

    // ─── Read / query endpoints ───────────────────────────────────────────────

    /// <summary>
    /// Lists the authenticated student's consultant bookings, newest first.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> GetMyBookings(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyBookingsQuery(), cancellationToken));

    /// <summary>
    /// Lists the authenticated consultant's own (incoming) bookings, newest first.
    /// </summary>
    [HttpGet("consultant")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> GetConsultantBookings(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetConsultantBookingsQuery(), cancellationToken));

    /// <summary>
    /// Returns the authenticated consultant's own active availability rules.
    /// </summary>
    [HttpGet("me/availability")]
    [ProducesResponseType(typeof(IReadOnlyList<AvailabilityRuleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvailabilityRuleDto>>> GetMyAvailability(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyAvailabilityQuery(), cancellationToken));

    /// <summary>
    /// Returns one booking's full detail. Authorized to the booking's student,
    /// its consultant, or an admin — otherwise 403.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BookingDetailDto>> GetBookingById(
        Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetBookingByIdQuery(id), cancellationToken));

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

        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
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

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelBooking(
        Guid id,
        [FromBody] CancelBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.BookingId)
        {
            return BadRequest("Route booking id does not match body booking id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// FR-229 — reschedule a requested or confirmed booking to a new time slot.
    /// No new payment is taken; the existing payment carries over.
    /// </summary>
    [HttpPost("{id:guid}/reschedule")]
    public async Task<IActionResult> RescheduleBooking(
        Guid id,
        [FromBody] RescheduleBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.BookingId)
        {
            return BadRequest("Route booking id does not match body booking id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/no-show")]
    public async Task<IActionResult> MarkNoShow(
        Guid id,
        [FromBody] MarkNoShowCommand command,
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

    [HttpPost("{id:guid}/rating")]
    public async Task<IActionResult> SubmitConsultantRating(
    Guid id,
    [FromBody] SubmitConsultantRatingCommand command,
    CancellationToken cancellationToken)
    {
        if (id != command.BookingId)
        {
            return BadRequest("Route booking id does not match body booking id.");
        }

        await _sender.Send(command, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Admin moderation (FR-101): hide a consultant review from public listings or
    /// un-hide a previously hidden one.
    /// </summary>
    [HttpPost("/api/consultant-reviews/{reviewId:guid}/moderate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> ModerateConsultantReview(
        Guid reviewId,
        [FromBody] ModerateConsultantReviewBody body,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new HideConsultantReviewCommand(reviewId, body.Hide, body.AdminNote),
            cancellationToken);
        return NoContent();
    }
}

// ─── Request DTOs kept local to the controller ────────────────────────────────
public sealed record ModerateConsultantReviewBody(bool Hide, string? AdminNote);
