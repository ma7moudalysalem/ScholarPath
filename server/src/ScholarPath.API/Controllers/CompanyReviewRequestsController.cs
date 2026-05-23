using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.CompanyReviewRequests.Commands.Accept;
using ScholarPath.Application.CompanyReviewRequests.Commands.Cancel;
using ScholarPath.Application.CompanyReviewRequests.Commands.Complete;
using ScholarPath.Application.CompanyReviewRequests.Commands.ConfirmHold;
using ScholarPath.Application.CompanyReviewRequests.Commands.Expire;
using ScholarPath.Application.CompanyReviewRequests.Commands.Reject;
using ScholarPath.Application.CompanyReviewRequests.Commands.Start;
using ScholarPath.Application.CompanyReviewRequests.DTOs;
using ScholarPath.Application.CompanyReviewRequests.Queries.GetById;
using ScholarPath.Application.CompanyReviewRequests.Queries.GetMyAsCompany;
using ScholarPath.Application.CompanyReviewRequests.Queries.GetMyAsStudent;

namespace ScholarPath.API.Controllers;

/// <summary>
/// PB-005 paid CompanyReview support flow. Apply Now → Submitted → (Stripe
/// confirm) → Pending → Company accept/reject → UnderReview → Completed.
/// Student-side cancellation is allowed in Submitted, Pending, and (with 50%
/// refund) UnderReview. Refund/Commission rules: spec PARTs 5-6.
/// </summary>
[ApiController]
[Route("api/company-review-requests")]
[Authorize]
public sealed class CompanyReviewRequestsController(ISender sender) : ControllerBase
{
    // ─── Student flow ─────────────────────────────────────────────────────────

    /// <summary>Apply Now — creates the request + Stripe PaymentIntent.</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(StartCompanyReviewRequestResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StartCompanyReviewRequestResult>> Start(
        [FromBody] StartCompanyReviewRequestCommand body, CancellationToken ct)
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
        await sender.Send(new ConfirmCompanyReviewRequestHoldCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Student cancels the request. Refund policy depends on current status:
    /// no charge before Company acceptance; 50% refund during UnderReview;
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
        await sender.Send(new CancelCompanyReviewRequestCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpGet("me/student")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyReviewRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyReviewRequestDto>>> ListMineAsStudent(
        CancellationToken ct)
        => Ok(await sender.Send(new GetMyCompanyReviewRequestsAsStudentQuery(), ct));

    // ─── Company flow ─────────────────────────────────────────────────────────

    [HttpGet("me/company")]
    [Authorize(Roles = "Company")]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyReviewRequestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CompanyReviewRequestDto>>> ListMineAsCompany(
        CancellationToken ct)
        => Ok(await sender.Send(new GetMyCompanyReviewRequestsAsCompanyQuery(), ct));

    [HttpPost("{id:guid}/accept")]
    [Authorize(Roles = "Company")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken ct)
    {
        await sender.Send(new AcceptCompanyReviewRequestCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Company")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectBody body, CancellationToken ct)
    {
        await sender.Send(new RejectCompanyReviewRequestCommand(id, body.Reason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/complete")]
    [Authorize(Roles = "Company")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(Guid id, CancellationToken ct)
    {
        await sender.Send(new CompleteCompanyReviewRequestCommand(id), ct);
        return NoContent();
    }

    // ─── Shared read ──────────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyReviewRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<CompanyReviewRequestDto>> Get(Guid id, CancellationToken ct)
        => Ok(await sender.Send(new GetCompanyReviewRequestByIdQuery(id), ct));

    // ─── Admin ────────────────────────────────────────────────────────────────

    /// <summary>Admin-only manual expire of a stuck Pending request.</summary>
    [HttpPost("{id:guid}/expire")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Expire(Guid id, CancellationToken ct)
    {
        await sender.Send(new ExpireCompanyReviewRequestCommand(id), ct);
        return NoContent();
    }
}

public sealed record CancelBody(string? Reason = null);
public sealed record RejectBody(string? Reason = null);
