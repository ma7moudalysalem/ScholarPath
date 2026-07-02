using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Accept;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Cancel;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Complete;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.ConfirmHold;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Expire;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Reject;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Commands.Start;
using ScholarPath.Application.ScholarshipProviderReviewRequests.DTOs;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetById;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetMyAsScholarshipProvider;
using ScholarPath.Application.ScholarshipProviderReviewRequests.Queries.GetMyAsStudent;

namespace ScholarPath.API.Controllers;

/// <summary>
/// PB-005 paid ScholarshipProviderReview support flow. Apply Now → Submitted → (Stripe
/// confirm) → Pending → ScholarshipProvider accept/reject → UnderReview → Completed.
/// Student-side cancellation is allowed in Submitted, Pending, and (with 50%
/// refund) UnderReview. Refund/Commission rules: spec PARTs 5-6.
/// </summary>
[ApiController]
[Route("api/company-review-requests")]
[Authorize]
public sealed class ScholarshipProviderReviewRequestsController(ISender sender) : ControllerBase
{
    // ─── Student flow ─────────────────────────────────────────────────────────

    /// <summary>Apply Now — creates the request + Stripe PaymentIntent.</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(StartScholarshipProviderReviewRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartScholarshipProviderReviewRequestResult>> Start(
        [FromBody] StartScholarshipProviderReviewRequestCommand body, CancellationToken ct)
        => Ok(await sender.Send(body, ct));

    /// <summary>
    /// Called by the Student app after Stripe Elements authorises the card.
    /// Moves the request Submitted → Pending and dispatches "payment held"
    /// notifications to both parties. Idempotent.
    /// </summary>
    [HttpPost("{id:guid}/confirm-payment")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ConfirmHold(Guid id, CancellationToken ct)
    {
        await sender.Send(new ConfirmScholarshipProviderReviewRequestHoldCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Student cancels the request. Refund policy depends on current status:
    /// no charge before ScholarshipProvider acceptance; 50% refund during UnderReview;
    /// refused after Completed.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(
        Guid id, [FromBody] CancelBody body, CancellationToken ct)
    {
        await sender.Send(new CancelScholarshipProviderReviewRequestCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpGet("me/student")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(IReadOnlyList<ScholarshipProviderReviewRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScholarshipProviderReviewRequestDto>>> ListMineAsStudent(
        CancellationToken ct)
        => Ok(await sender.Send(new GetMyScholarshipProviderReviewRequestsAsStudentQuery(), ct));

    // ─── ScholarshipProvider flow ─────────────────────────────────────────────────────────

    [HttpGet("me/company")]
    [Authorize(Roles = "ScholarshipProvider")]
    [ProducesResponseType(typeof(IReadOnlyList<ScholarshipProviderReviewRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ScholarshipProviderReviewRequestDto>>> ListMineAsScholarshipProvider(
        CancellationToken ct)
        => Ok(await sender.Send(new GetMyScholarshipProviderReviewRequestsAsScholarshipProviderQuery(), ct));

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "ScholarshipProvider")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        await sender.Send(new AcceptScholarshipProviderReviewRequestCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "ScholarshipProvider")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectBody body, CancellationToken ct)
    {
        await sender.Send(new RejectScholarshipProviderReviewRequestCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "ScholarshipProvider")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await sender.Send(new CompleteScholarshipProviderReviewRequestCommand(id), ct);
        return NoContent();
    }

    // ─── Shared read ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ScholarshipProviderReviewRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ScholarshipProviderReviewRequestDto>> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetScholarshipProviderReviewRequestByIdQuery(id), ct));

    // ─── Admin ────────────────────────────────────────────────────────────────

    /// <summary>Admin-only manual expire of a stuck Pending request.</summary>
    [HttpPost("{id:guid}/expire")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Expire(Guid id, CancellationToken ct)
    {
        await sender.Send(new ExpireScholarshipProviderReviewRequestCommand(id), ct);
        return NoContent();
    }
}

public sealed record CancelBody(string? Reason = null);
public sealed record RejectBody(string? Reason = null);
