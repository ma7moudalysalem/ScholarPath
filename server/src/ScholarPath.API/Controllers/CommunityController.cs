using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Community.Commands.CreateCategory;
using ScholarPath.Application.Community.Commands.CreatePost;
using ScholarPath.Application.Community.Commands.CreateReply;
using ScholarPath.Application.Community.Commands.FlagPost;
using ScholarPath.Application.Community.Commands.ToggleVote;
using ScholarPath.Application.Community.Queries.GetCategories;
using ScholarPath.Application.Community.Queries.GetPostDetails;
using ScholarPath.Application.Community.Queries.GetPosts;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommunityController(IMediator mediator) : ControllerBase
{
    [HttpGet("categories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategories(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetCategoriesQuery(), ct));
    }

    [HttpGet("posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPosts(
        [FromQuery] Guid? categoryId,
        [FromQuery] string? query,
        [FromQuery] string sortBy = "Newest",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetPostsQuery(categoryId, query, sortBy, page, pageSize), ct));
    }

    [HttpGet("posts/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPostDetails(Guid id, CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetPostDetailsQuery(id), ct));
    }

    [HttpPost("posts")]
    public async Task<IActionResult> CreatePost([FromBody] CreatePostRequest request, CancellationToken ct)
    {
        var command = new CreatePostCommand(request.CategoryId, request.Title, request.BodyMarkdown);
        var id = await mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetPostDetails), new { id }, id);
    }

    [HttpPost("posts/{id:guid}/replies")]
    public async Task<IActionResult> CreateReply(Guid id, [FromBody] CreateReplyRequest request, CancellationToken ct)
    {
        var command = new CreateReplyCommand(id, request.BodyMarkdown);
        var replyId = await mediator.Send(command, ct);
        return Ok(replyId);
    }

    [HttpPost("posts/{id:guid}/vote")]
    public async Task<IActionResult> ToggleVote(Guid id, [FromBody] ToggleVoteRequest request, CancellationToken ct)
    {
        var command = new ToggleVoteCommand(id, request.VoteType);
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpPost("posts/{id:guid}/flag")]
    public async Task<IActionResult> FlagPost(Guid id, [FromBody] FlagPostRequest request, CancellationToken ct)
    {
        var command = new FlagPostCommand(id, request.Reason, request.AdditionalDetails);
        await mediator.Send(command, ct);
        return NoContent();
    }
}

public record CreatePostRequest(Guid CategoryId, string Title, string BodyMarkdown);
public record CreateReplyRequest(string BodyMarkdown);
public record ToggleVoteRequest(ScholarPath.Domain.Enums.VoteType VoteType);
public record FlagPostRequest(string Reason, string? AdditionalDetails);
