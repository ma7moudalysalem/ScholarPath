using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.CompanyReviews.Commands.SubmitCompanyRating;
using ScholarPath.Application.CompanyReviews.Queries.GetCompanyRatings;

namespace ScholarPath.API.Controllers;

[ApiController]
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
}
