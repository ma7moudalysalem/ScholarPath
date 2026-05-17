using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Application.Scholarships.Commands.ApproveScholarship;
using ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;
using ScholarPath.Application.Scholarships.Commands.RejectScholarship;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Application.Scholarships.Queries.GetMyScholarships;
using ScholarPath.Application.Scholarships.Queries.GetScholarshipsForModeration;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers
{
    [ApiController]
    [Route("api/scholarships")] //  Convention route
    public class ScholarshipsController(IMediator mediator) : ControllerBase //  ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<PaginatedList<ScholarshipDto>>> Get([FromQuery] GetScholarshipsQuery query)
        {
            //  Reading language from Accept-Language header
            var headerLang = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
             var lang = headerLang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

            var updatedQuery = query with { Language = lang };

            return await mediator.Send(updatedQuery);
        }
        

        [HttpGet("{id}")]
        public async Task<ActionResult<ScholarshipDetailDto>> GetById(Guid id, [FromQuery] string? language)
        {
            var headerValue = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
            var detectedLang = headerValue. StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";
            var lang = language ?? detectedLang;

            return await mediator.Send(new GetScholarshipByIdQuery(id, lang));
        }

        //  Post/Put/Delete methods with [Authorize] will be added here in the next step
        [HttpPost]
        [Authorize(Roles = "Company")] //  Authorization enforced
        public async Task<ActionResult<Guid>> Create(CreateScholarshipCommand command)
        {
            return await mediator.Send(command);
        }

        [HttpPost("{id}/bookmark")]
        [Authorize] // Any logged in user
        public async Task<ActionResult<bool>> ToggleBookmark(Guid id)
        {
            return await mediator.Send(new BookmarkToggleCommand(id));
        }

        // PB-005: company configures the per-scholarship review fee.
        [HttpPost("{id:guid}/review-fee")]
        [Authorize(Roles = "Company,Admin")]
        public async Task<IActionResult> ConfigureReviewFee(
            Guid id, [FromBody] ConfigureReviewFeeRequest request, CancellationToken ct)
        {
            var result = await mediator.Send(new ConfigureReviewFeeCommand(id, request.ReviewFeeUsd), ct);
            return result ? Ok() : BadRequest();
        }

        // ── Company: my own scholarships ─────────────────────────────────────
        [HttpGet("mine")]
        [Authorize(Roles = "Company")]
        [ProducesResponseType(typeof(IReadOnlyList<MyScholarshipDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Mine(CancellationToken ct)
            => Ok(await mediator.Send(new GetMyScholarshipsQuery(), ct));

        // ── Admin moderation ─────────────────────────────────────────────────
        [HttpGet("admin")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(PaginatedList<MyScholarshipDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminList(
            [FromQuery] ScholarshipStatus status = ScholarshipStatus.UnderReview,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
            => Ok(await mediator.Send(
                new GetScholarshipsForModerationQuery(status, page, pageSize), ct));

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
        {
            await mediator.Send(new ApproveScholarshipCommand(id), ct);
            return NoContent();
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(
            Guid id, [FromBody] RejectScholarshipBody body, CancellationToken ct)
        {
            await mediator.Send(new RejectScholarshipCommand(id, body.Reason), ct);
            return NoContent();
        }
    }

    public record ConfigureReviewFeeRequest(decimal ReviewFeeUsd);
    public record RejectScholarshipBody(string Reason);
}
