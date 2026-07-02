using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Common.Models;
using ScholarPath.Application.Resources;
using ScholarPath.Application.Resources.Commands.ApproveResource;
using ScholarPath.Application.Resources.Commands.CompleteResourceChapter;
using ScholarPath.Application.Resources.Commands.CreateResource;
using ScholarPath.Application.Resources.Commands.FeatureResource;
using ScholarPath.Application.Resources.Commands.RejectResource;
using ScholarPath.Application.Resources.Commands.SetResourceVisibility;
using ScholarPath.Application.Resources.Commands.SubmitResourceForReview;
using ScholarPath.Application.Resources.Commands.ToggleResourceBookmark;
using ScholarPath.Application.Resources.Commands.UpdateResource;
using ScholarPath.Application.Resources.Queries.GetFeaturedResources;
using ScholarPath.Application.Resources.Queries.GetMyResourceBookmarks;
using ScholarPath.Application.Resources.Queries.GetMyResourceProgress;
using ScholarPath.Application.Resources.Queries.GetMyResources;
using ScholarPath.Application.Resources.Queries.GetPendingReviewResources;
using ScholarPath.Application.Resources.Queries.GetResourceDetail;
using ScholarPath.Application.Resources.Queries.SearchResources;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers;

/// <summary>Resources Hub (PB-009) — public browse, author CRUD, student progress, admin moderation.</summary>
[ApiController]
[Authorize]
[Route("api/resources")]
[Produces("application/json")]
public sealed class ResourcesController(IMediator mediator) : ControllerBase
{
    // ── Public read ───────────────────────────────────────────────────────────

    /// <summary>Browse/search published resources.</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PaginatedList<ResourceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] SearchResourcesQuery query, CancellationToken ct)
        => Ok(await mediator.Send(query, ct));

    /// <summary>
    /// Canonical list of category slugs the platform recognises. Authoring
    /// clients use this to render the dropdown so the validator and the UI
    /// stay in sync.
    /// </summary>
    [HttpGet("categories")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult Categories()
        => Ok(ResourceCategoryCatalog.Slugs);

    /// <summary>Featured resources for the homepage hub.</summary>
    [HttpGet("featured")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Featured(CancellationToken ct)
        => Ok(await mediator.Send(new GetFeaturedResourcesQuery(), ct));

    /// <summary>Full resource detail by id or slug.</summary>
    [HttpGet("{idOrSlug}")]
    [Authorize]
    [ProducesResponseType(typeof(ResourceDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdOrSlug(string idOrSlug, CancellationToken ct)
    {
        var result = await mediator.Send(new GetResourceDetailQuery(idOrSlug), ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Author CRUD ───────────────────────────────────────────────────────────

    [HttpPost]
    [Authorize(Roles = "Consultant,ScholarshipProvider,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateResourceCommand command, CancellationToken ct)
    {
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetByIdOrSlug), new { idOrSlug = id.ToString() }, id);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Consultant,ScholarshipProvider,Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateResourceCommand command, CancellationToken ct)
    {
        await mediator.Send(command with { ResourceId = id }, ct);
        return NoContent();
    }

    /// <summary>Submits a draft for review (or publishes directly if the caller is an admin).</summary>
    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = "Consultant,ScholarshipProvider,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(ResourceStatus), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Submit(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new SubmitResourceForReviewCommand(id), ct));

    /// <summary>The caller's own resources, any status.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Consultant,ScholarshipProvider,Admin,SuperAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyResourcesQuery(), ct));

    // ── Student bookmark + progress ───────────────────────────────────────────

    [HttpPost("{id:guid}/bookmark")]
    [Authorize]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ToggleBookmark(Guid id, CancellationToken ct)
        => Ok(await mediator.Send(new ToggleResourceBookmarkCommand(id), ct));

    [HttpGet("bookmarks/me")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceBookmarkDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyBookmarks(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyResourceBookmarksQuery(), ct));

    [HttpPost("{id:guid}/chapters/{chapterId:guid}/complete")]
    [Authorize]
    [ProducesResponseType(typeof(ChapterProgressResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteChapter(Guid id, Guid chapterId, CancellationToken ct)
        => Ok(await mediator.Send(new CompleteResourceChapterCommand(id, chapterId), ct));

    [HttpGet("progress/me")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> MyProgress(CancellationToken ct)
        => Ok(await mediator.Send(new GetMyResourceProgressQuery(), ct));

    // ── Admin moderation ──────────────────────────────────────────────────────

    [HttpGet("pending-review")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(typeof(IReadOnlyList<ResourceListItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PendingReview(CancellationToken ct)
        => Ok(await mediator.Send(new GetPendingReviewResourcesQuery(), ct));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken ct)
    {
        await mediator.Send(new ApproveResourceCommand(id), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid id, [FromBody] RejectResourceBody body, CancellationToken ct)
    {
        await mediator.Send(new RejectResourceCommand(id, body.RejectionReason), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/feature")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Feature(
        Guid id, [FromBody] FeatureResourceBody body, CancellationToken ct)
    {
        await mediator.Send(new FeatureResourceCommand(id, body.Featured), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/visibility")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetVisibility(
        Guid id, [FromBody] SetVisibilityBody body, CancellationToken ct)
    {
        await mediator.Send(new SetResourceVisibilityCommand(id, body.Status), ct);
        return NoContent();
    }
}

// ─── Request DTOs kept local to the controller ────────────────────────────────
public sealed record RejectResourceBody(string RejectionReason);
public sealed record FeatureResourceBody(bool Featured);
public sealed record SetVisibilityBody(ResourceStatus Status);
