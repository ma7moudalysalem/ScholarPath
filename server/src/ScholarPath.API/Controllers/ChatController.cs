using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Chat.Commands.BlockUser;
using ScholarPath.Application.Chat.Commands.SendMessage;
using ScholarPath.Application.Chat.Commands.UnblockUser;
using ScholarPath.Application.Chat.Queries.GetConversations;
using ScholarPath.Application.Chat.Queries.GetMessages;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController(IMediator mediator) : ControllerBase
{
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
    {
        return Ok(await mediator.Send(new GetConversationsQuery(), ct));
    }

    [HttpGet("conversations/{id:guid}/messages")]
    public async Task<IActionResult> GetMessages(
        Guid id,
        [FromQuery] int limit = 50,
        [FromQuery] DateTimeOffset? before = null,
        CancellationToken ct = default)
    {
        return Ok(await mediator.Send(new GetMessagesQuery(id, limit, before), ct));
    }

    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request, CancellationToken ct)
    {
        var command = new SendMessageCommand(request.RecipientId, request.Body);
        var result = await mediator.Send(command, ct);
        return Ok(result);
    }

    [HttpPost("blocks")]
    public async Task<IActionResult> BlockUser([FromBody] BlockUserRequest request, CancellationToken ct)
    {
        var command = new BlockUserCommand(request.UserId, request.Reason);
        await mediator.Send(command, ct);
        return NoContent();
    }

    [HttpDelete("blocks/{userId:guid}")]
    public async Task<IActionResult> UnblockUser(Guid userId, CancellationToken ct)
    {
        var command = new UnblockUserCommand(userId);
        await mediator.Send(command, ct);
        return NoContent();
    }
}

public record SendMessageRequest(Guid RecipientId, string Body);
public record BlockUserRequest(Guid UserId, string? Reason);
