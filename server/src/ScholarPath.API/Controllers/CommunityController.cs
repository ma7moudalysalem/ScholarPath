using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Community.Commands.CreatePost;
using ScholarPath.Application.Community.Commands.CreateReply;
using ScholarPath.Application.Community.Commands.DeletePost;
using ScholarPath.Application.Community.Commands.DismissPostFlags;
using ScholarPath.Application.Community.Commands.FlagPost;
using ScholarPath.Application.Community.Commands.ToggleBookmark;
using ScholarPath.Application.Community.Commands.ToggleVote;
using ScholarPath.Application.Community.Commands.UpdatePost;
using ScholarPath.Application.Community.Queries.GetCategories;
using ScholarPath.Application.Community.Queries.GetFlaggedPosts;
using ScholarPath.Application.Community.Queries.GetMyBookmarks;
using ScholarPath.Application.Community.Queries.GetPostDetails;
using ScholarPath.Application.Community.Queries.GetPosts;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommunityController(IMediator mediator) : ControllerBase
{
    // ── Public reads ───────────────────────────────────────────────────────────

    [HttpGet("categories")]
    [Authorize]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCategoriesQuery(), ct));
    }

    [HttpGet("posts")]
    [Authorize]
    public async Task<IActionResult> GetPosts(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? query,
        [FromQuery] string sortBy = "Newest",
        [FromQuery] string? tag = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetPostsQuery(categoryId, query, sortBy, tag, false, page, pageSize), ct));
    }

    [HttpGet("posts/{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetPostDetails(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetPostDetailsQuery(id), ct));
    }

    // ── Student writes ─────────────────────────────────────────────────────────

    [HttpPost("posts")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var command = new CreatePostCommand(request.CategoryId, request.Title, request.BodyMarkdown, request.Tags);
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetPostDetails), new { id }, id);
    }

    [HttpPut("posts/{id:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> UpdatePost(Guid id, [FromBody] UpdatePostRequest request, CancellationToken ct)
    {
        var command = new UpdatePostCommand(id, request.Title, request.BodyMarkdown, request.Tags);
        await mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Student deletes their own post or reply. Admin / SuperAdmin moderation
    /// goes through <c>POST /admin/posts/{id}/remove</c> instead so the
    /// user-facing delete stays a pure author-owned operation.
    /// </summary>
    [HttpDelete("posts/{id:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> DeletePost(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeletePostCommand(id), ct);
        return NoContent();
    }

    [HttpPost("posts/{id:guid}/replies")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> CreateReply(Guid id, [FromBody] CreateReplyRequest request, CancellationToken ct)
    {
        var command = new CreateReplyCommand(id, request.BodyMarkdown);
        var replyId = await mediator.Send(command, ct);
        return Ok(replyId);
    }

    [HttpPost("posts/{id:guid}/vote")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ToggleVote(Guid id, [FromBody] ToggleVoteRequest request, CancellationToken ct)
    {
        var command = new ToggleVoteCommand(id, request.VoteType);
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("posts/{id:guid}/flag")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> FlagPost(Guid id, [FromBody] FlagPostRequest request, CancellationToken ct)
    {
        var command = new FlagPostCommand(id, request.Reason, request.AdditionalDetails);
        await mediator.Send(command, ct);
        return NoContent();
    }

    // ── Student bookmarks ──────────────────────────────────────────────────────

    /// <summary>
    /// Toggles a bookmark on a root post. Returns <c>{ bookmarked: true }</c>
    /// when the post is now saved, <c>false</c> when it was removed.
    /// </summary>
    [HttpPost("posts/{id:guid}/bookmark")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ToggleBookmark(Guid id, CancellationToken ct)
    {
        var bookmarked = await mediator.Send(new ToggleBookmarkCommand(id), ct);
        return Ok(new { bookmarked });
    }

    [HttpGet("bookmarks")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyBookmarks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetMyBookmarksQuery(page, pageSize), ct));

    // ── Admin moderation ───────────────────────────────────────────────────────

    /// <summary>Lists posts in the moderation queue — flagged or auto-hidden.</summary>
    [HttpGet("admin/flagged")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> GetFlagged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await mediator.Send(new GetFlaggedPostsQuery(page, pageSize), ct));

    /// <summary>Removes a flagged post (soft-delete).</summary>
    [HttpPost("admin/posts/{id:guid}/remove")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> RemovePost(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new DeletePostCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }

    /// <summary>Dismisses the flags on a post ("keep") — clears the moderation state.</summary>
    [HttpPost("admin/posts/{id:guid}/dismiss")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> DismissFlags(Guid id, CancellationToken ct)
    {
        var ok = await mediator.Send(new DismissPostFlagsCommand(id), ct);
        return ok ? NoContent() : NotFound();
    }
}

public record CreatePostRequest(Guid CategoryId, string Title, string BodyMarkdown, IReadOnlyList<string>? Tags = null);
public record UpdatePostRequest(string? Title, string BodyMarkdown, IReadOnlyList<string>? Tags = null);
public record CreateReplyRequest(string BodyMarkdown);
public record ToggleVoteRequest(ScholarPath.Domain.Enums.VoteType VoteType);
public record FlagPostRequest(string Reason, string? AdditionalDetails);
