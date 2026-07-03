using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ConsultantBookings.Commands.AcceptBooking;
using ScholarPath.Application.ConsultantBookings.Commands.CancelBooking;
using ScholarPath.Application.ConsultantBookings.Commands.HideConsultantReview;
using ScholarPath.Application.ConsultantBookings.Commands.MarkNoShow;
using ScholarPath.Application.ConsultantBookings.Commands.RecordMeetingJoin;
using ScholarPath.Application.ConsultantBookings.Commands.RejectBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RequestBooking;
using ScholarPath.Application.ConsultantBookings.Commands.RescheduleBooking;
using ScholarPath.Application.ConsultantBookings.Commands.StartMeetingRecording;
using ScholarPath.Application.ConsultantBookings.Commands.UpdateAvailability;
using ScholarPath.Application.ConsultantBookings.Commands.SubmitConsultantRating;
using ScholarPath.Application.ConsultantBookings.DTOs;
using ScholarPath.Application.ConsultantBookings.Queries.DownloadSessionRecording;
using ScholarPath.Application.ConsultantBookings.Queries.GetBookingById;
using ScholarPath.Application.ConsultantBookings.Queries.GetBookingRecordings;
using ScholarPath.Application.ConsultantBookings.Queries.GetConsultantBookings;
using ScholarPath.Application.ConsultantBookings.Queries.GetAllBookings;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyAvailability;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyBookings;
using ScholarPath.Application.ConsultantBookings.Queries.GetMyReceivedReviews;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;

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
    /// Lists all bookings platform-wide — admin users only. A ScholarshipProvider
    /// has no relationship to any booking (the ConsultantBooking entity has only
    /// StudentId + ConsultantId), so it must not see other parties' bookings.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<BookingListItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<BookingListItemDto>>> GetAllBookings(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetAllBookingsQuery(), cancellationToken));

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
    /// Returns the authenticated consultant's own received reviews — masked
    /// author names, admin-hidden and soft-deleted rows excluded, newest first —
    /// plus an aggregate average and count. Backs the consultant "Reviews
    /// received" page.
    /// </summary>
    [HttpGet("/api/consultant/reviews/mine")]
    [Authorize(Roles = "Consultant")]
    [ProducesResponseType(typeof(ReceivedReviewsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceivedReviewsSummaryDto>> GetMyReceivedReviews(
        CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetMyReceivedConsultantReviewsQuery(), cancellationToken));

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

    /// <summary>
    /// FR-217 — records that the authenticated participant joined the booking's
    /// session room. This attendance signal is what the automated no-show sweep
    /// reads to attribute a no-show to whichever party never joined.
    /// </summary>
    [HttpPost("{id:guid}/meeting/join")]
    [ProducesResponseType(typeof(MeetingJoinResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> JoinMeeting(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new RecordMeetingJoinCommand(id), cancellationToken));

    /// <summary>
    /// PB-006 — starts recording the booking's video session. Idempotent; the
    /// client supplies the live call's server call id.
    /// </summary>
    [HttpPost("{id:guid}/meeting/start-recording")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartMeetingRecording(
        Guid id, [FromBody] StartRecordingBody body, CancellationToken cancellationToken)
    {
        await _sender.Send(new StartMeetingRecordingCommand(id, body.ServerCallId), cancellationToken);
        return NoContent();
    }

    /// <summary>Lists a booking's session recordings — booking student, consultant, or admin.</summary>
    [HttpGet("{id:guid}/meeting/recordings")]
    [ProducesResponseType(typeof(IReadOnlyList<SessionRecordingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMeetingRecordings(Guid id, CancellationToken cancellationToken)
        => Ok(await _sender.Send(new GetBookingRecordingsQuery(id), cancellationToken));

    /// <summary>Streams a session recording — booking student, consultant, or admin.</summary>
    [HttpGet("meeting/recordings/{recordingId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadMeetingRecording(
        Guid recordingId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DownloadSessionRecordingQuery(recordingId), cancellationToken);
        return File(result.Content, result.ContentType, result.FileName);
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
public sealed record StartRecordingBody(string ServerCallId);
