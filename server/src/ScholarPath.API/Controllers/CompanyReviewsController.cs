using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.CompanyReviews.Commands.HideCompanyReview;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using ScholarPath.Application.CompanyReviews.DTOs;
using ScholarPath.Application.CompanyReviews.Queries.GetCompanyRatings;
using ScholarPath.Application.CompanyReviews.Queries.GetMyReceivedReviews;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize]
[Route("api/company-reviews")]
public class CompanyReviewsController(IMediator mediator) : ControllerBase
{
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> SubmitCompanyRating([FromBody] SubmitCompanyRatingCommand command, CancellationToken ct)
    {
        var reviewId = await mediator.Send(command, ct);
        return Ok(new { ReviewId = reviewId });
    }

    [HttpGet("~/api/companies/{companyId:guid}/reviews")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyRatings(Guid companyId, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var query = new GetCompanyRatingsQuery(companyId, page, pageSize);
        var result = await mediator.Send(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns the authenticated company's own received reviews — masked author
    /// names, admin-hidden and soft-deleted rows excluded, newest first — plus an
    /// aggregate average and count. Backs the company "Reviews received" page.
    /// </summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Company")]
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
    public async Task<IActionResult> ModerateCompanyReview(
        Guid reviewId, [FromBody] ModerateReviewBody body, CancellationToken ct)
    {
        await mediator.Send(
            new HideCompanyReviewCommand(reviewId, body.Hide, body.AdminNote), ct);
        return NoContent();
    }
}

// ─── Request DTOs kept local to the controller ────────────────────────────────
public sealed record ModerateReviewBody(bool Hide, string? AdminNote);
