using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.HideScholarshipProviderReview;
using ScholarPath.Application.ScholarshipProviderReviews.Commands.SubmitScholarshipProviderRating;
using ScholarPath.Application.ScholarshipProviderReviews.DTOs;
using ScholarPath.Application.ScholarshipProviderReviews.Queries.GetScholarshipProviderRatings;
using ScholarPath.Application.ScholarshipProviderReviews.Queries.GetMyReceivedReviews;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize]
[Route("api/company-reviews")]
public class ScholarshipProviderReviewsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitScholarshipProviderRating([FromBody] SubmitScholarshipProviderRatingCommand command, CancellationToken ct)
    {
        var reviewId = await mediator.Send(command, ct);
        return Ok(new { ReviewId = reviewId });
    }

    [HttpGet("~/api/companies/{companyId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetScholarshipProviderRatings(Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var query = new GetScholarshipProviderRatingsQuery(companyId, page, pageSize);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the authenticated company's own received reviews — masked author
    /// names, admin-hidden and soft-deleted rows excluded, newest first — plus an
    /// aggregate average and count. Backs the company "Reviews received" page.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "ScholarshipProvider")]
    [ProducesResponseType(typeof(ReceivedReviewsSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReceivedReviewsSummaryDto>> GetMyReceivedReviews(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyReceivedReviewsQuery(), ct));

    /// <summary>
    /// Admin moderation (FR-075): hide a company review from public listings or
    /// un-hide a previously hidden one.
    /// </summary>
    [HttpPost("{reviewId:guid}/moderate")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ModerateScholarshipProviderReview(
        Guid reviewId, [FromBody] ModerateReviewBody body, CancellationToken ct)
    {
        await mediator.Send(
            new HideScholarshipProviderReviewCommand(reviewId, body.Hide, body.AdminNote), ct);
        return NoContent();
    }
}

// ─── Request DTOs kept local to the controller ────────────────────────────────
public sealed record ModerateReviewBody(bool Hide, string? AdminNote);
