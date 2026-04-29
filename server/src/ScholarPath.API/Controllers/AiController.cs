using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Ai.Commands.AskChatbot;
using ScholarPath.Application.Ai.Commands.CheckEligibility;
using ScholarPath.Application.Ai.Commands.GenerateRecommendations;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Ai.Queries.GetMyInteractions;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
[Produces("application/json")]
public sealed class AiController(IMediator mediator) : ControllerBase
{
    [HttpPost("recommendations")]
    [ProducesResponseType(typeof(RecommendationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Recommendations(
        [FromQuery] int? topN,
        CancellationToken ct)
    {
        var result = await mediator.Send(new GenerateRecommendationsCommand(topN), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("eligibility/{scholarshipId:guid}")]
    [ProducesResponseType(typeof(EligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Eligibility(Guid scholarshipId, CancellationToken ct)
    {
        var result = await mediator.Send(new CheckEligibilityCommand(scholarshipId), ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpPost("chat")]
    [ProducesResponseType(typeof(ChatAnswerDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Chat([FromBody] AskChatbotCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("interactions")]
    [ProducesResponseType(typeof(IReadOnlyList<AiInteractionRowDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Interactions([FromQuery] int limit = 20, CancellationToken ct = default)
    {
        var rows = await mediator.Send(new GetMyInteractionsQuery(limit), ct).ConfigureAwait(false);
        return Ok(rows);
    }
}
