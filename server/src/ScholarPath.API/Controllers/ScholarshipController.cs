using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Scholarships.Commands;
using ScholarPath.Application.Scholarships.Commands.ApproveScholarship;
using ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;
using ScholarPath.Application.Scholarships.Commands.RejectScholarship;
using ScholarPath.Application.Scholarships.Commands.ReorderFeaturedScholarships;
using ScholarPath.Application.Scholarships.Commands.ToggleFeatureScholarship;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries;
using ScholarPath.Application.Scholarships.Queries.GetMyScholarships;
using ScholarPath.Application.Scholarships.Queries.GetScholarshipsForModeration;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers
{
    [ApiController]
    [Route("api/scholarships")] //  Convention route
    public class ScholarshipsController(IMediator mediator, IApplicationDbContext db) : ControllerBase //  ControllerBase
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

        // ── Public: featured scholarships (home page / dashboards) ───────────
        [HttpGet("featured")]
        [ProducesResponseType(typeof(IReadOnlyList<ScholarshipDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ScholarshipDto>>> Featured(
            [FromQuery] int? limit, CancellationToken ct)
        {
            var headerLang = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
            var lang = headerLang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

            var query = new GetFeaturedScholarshipsQuery { Language = lang };
            if (limit.HasValue) query = query with { Limit = limit.Value };

            return Ok(await mediator.Send(query, ct));
        }

        // ── Student: my bookmarked scholarships ──────────────────────────────
        [HttpGet("bookmarks")]
        [Authorize]
        [ProducesResponseType(typeof(IReadOnlyList<BookmarkedScholarshipDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<BookmarkedScholarshipDto>>> Bookmarks(CancellationToken ct)
        {
            var headerLang = Request.Headers["Accept-Language"].ToString().Split(',').FirstOrDefault() ?? "en";
            var lang = headerLang.StartsWith("ar", StringComparison.OrdinalIgnoreCase) ? "ar" : "en";

            return Ok(await mediator.Send(new GetMyBookmarkedScholarshipsQuery { Language = lang }, ct));
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

        // Company edits one of its own listings — title/description/category/deadline only.
        // FundingType and TargetLevel are create-only; the update command doesn't accept them.
        [HttpPut("{id:guid}")]
        [Authorize(Roles = "Company")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Update(
            Guid id, [FromBody] UpdateScholarshipBody body, CancellationToken ct)
        {
            await mediator.Send(new UpdateScholarshipCommand
            {
                Id = id,
                TitleEn = body.TitleEn,
                TitleAr = body.TitleAr,
                DescriptionEn = body.DescriptionEn,
                DescriptionAr = body.DescriptionAr,
                Deadline = body.Deadline,
                CategoryId = body.CategoryId,
                FieldsOfStudy = body.FieldsOfStudy,
            }, ct);
            return NoContent();
        }

        // Soft-delete (archive) — owning company or any Admin.
        [HttpDelete("{id:guid}")]
        [Authorize(Roles = "Company,Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Archive(Guid id, CancellationToken ct)
        {
            await mediator.Send(new ArchiveScholarshipCommand(id), ct);
            return NoContent();
        }

        // Categories list used to populate the scholarship-form dropdown.
        [HttpGet("categories")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(IReadOnlyList<ScholarshipCategoryDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<ScholarshipCategoryDto>>> Categories(CancellationToken ct)
        {
            var categories = await db.Categories
                .OrderBy(c => c.DisplayOrder)
                .Select(c => new ScholarshipCategoryDto(c.Id, c.NameEn, c.NameAr, c.Slug))
                .ToListAsync(ct);
            return Ok(categories);
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

        // ── Admin: featured scholarships ─────────────────────────────────────

        /// <summary>
        /// Admin-only list of ALL currently-featured scholarships (any status),
        /// ordered by FeaturedOrder. Used to populate the drag-to-reorder page.
        /// </summary>
        [HttpGet("admin/featured")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(typeof(IReadOnlyList<AdminFeaturedScholarshipDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdminFeatured(CancellationToken ct)
        {
            var rows = await db.Scholarships
                .AsNoTracking()
                .Where(s => s.IsFeatured && !s.IsDeleted)
                .OrderBy(s => s.FeaturedOrder)
                .ThenBy(s => s.Id)
                .Select(s => new AdminFeaturedScholarshipDto(
                    s.Id,
                    s.TitleEn,
                    s.TitleAr,
                    s.Status.ToString(),
                    s.FeaturedOrder,
                    s.Deadline))
                .ToListAsync(ct);
            return Ok(rows);
        }

        /// <summary>
        /// Admin-only: feature or un-feature a scholarship.
        /// Featuring requires the scholarship to be <c>Open</c> and the featured
        /// count to be below 12 (FR-030).
        /// </summary>
        [HttpPost("{id:guid}/feature")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ToggleFeature(
            Guid id, [FromBody] ToggleFeatureBody body, CancellationToken ct)
        {
            await mediator.Send(
                new ToggleFeatureScholarshipCommand(id, body.Featured), ct);
            return Ok();
        }

        /// <summary>
        /// Admin-only: overwrite the display order of ALL currently-featured
        /// scholarships in a single atomic operation (FR-030).
        /// </summary>
        [HttpPut("featured/reorder")]
        [Authorize(Roles = "Admin,SuperAdmin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ReorderFeatured(
            [FromBody] ReorderFeaturedBody body, CancellationToken ct)
        {
            await mediator.Send(
                new ReorderFeaturedScholarshipsCommand(body.Ids), ct);
            return NoContent();
        }

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
    public record ToggleFeatureBody(bool Featured);
    public record ReorderFeaturedBody(IReadOnlyList<Guid> Ids);

    public record UpdateScholarshipBody(
        string TitleEn,
        string TitleAr,
        string DescriptionEn,
        string DescriptionAr,
        DateTimeOffset Deadline,
        Guid CategoryId,
        string[]? FieldsOfStudy = null);

    public record ScholarshipCategoryDto(Guid Id, string NameEn, string NameAr, string Slug);

    /// <summary>Row used by the admin Featured-Scholarships drag-to-reorder page.</summary>
    public record AdminFeaturedScholarshipDto(
        Guid Id,
        string TitleEn,
        string TitleAr,
        string Status,
        int FeaturedOrder,
        DateTimeOffset Deadline);
}
