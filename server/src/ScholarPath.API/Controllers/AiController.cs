using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Ai.Commands.AskChatbot;
using ScholarPath.Application.Ai.Queries.GetAiSessionTurns;
using ScholarPath.Application.Ai.Queries.GetMyAiSessions;
using ScholarPath.Application.Ai.Commands.CheckEligibility;
using ScholarPath.Application.Ai.Commands.GenerateRecommendations;
using ScholarPath.Application.Ai.Commands.LogRecommendationClick;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Ai.Queries.GetMyInteractions;
using ScholarPath.Application.Ai.Queries.GetMyRecommendations;

namespace ScholarPath.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ai")]
[Produces("application/json")]
public sealed class AiController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Returns the user's cached recommendations (last 24h) without hitting the
    /// provider. Returns 204 when there's no cache — client should POST to regenerate.
    /// </summary>
    [HttpGet("recommendations")]
    [ProducesResponseType(typeof(RecommendationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetCachedRecommendations(
        [FromQuery] int maxAgeHours = 24,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetMyRecommendationsQuery(maxAgeHours), ct).ConfigureAwait(false);
        return result is null ? NoContent() : Ok(result);
    }

    /// <summary>
    /// Regenerates recommendations — writes a new AiInteraction row and counts
    /// against the daily cost budget.
    /// </summary>
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

    /// <summary>
    /// Lists the authenticated user's past chatbot sessions, newest-first.
    /// Backs the sidebar of past chats so the user can resume any of them —
    /// the assistant uses the session's turns as conversation memory.
    /// </summary>
    [HttpGet("chat/sessions")]
    [ProducesResponseType(typeof(IReadOnlyList<AiSessionSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Sessions(CancellationToken ct = default)
    {
        var sessions = await mediator.Send(new GetMyAiSessionsQuery(), ct).ConfigureAwait(false);
        return Ok(sessions);
    }

    /// <summary>
    /// Returns every persisted turn (prompt + response) of one chat session,
    /// scoped to the authenticated user, oldest-first. 404 is never returned —
    /// an unknown / cross-user session simply yields an empty list.
    /// </summary>
    [HttpGet("chat/sessions/{sessionId}")]
    [ProducesResponseType(typeof(IReadOnlyList<AiSessionTurnDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SessionTurns(string sessionId, CancellationToken ct = default)
    {
        var turns = await mediator
            .Send(new GetAiSessionTurnsQuery(sessionId), ct)
            .ConfigureAwait(false);
        return Ok(turns);
    }

    /// <summary>
    /// Logs that the student opened a scholarship surfaced by the recommender.
    /// Powers the CTR widget on the AI-economy dashboard (PB-017 / FR-249).
    /// Same-scholarship repeat clicks inside 500ms are deduplicated server-side,
    /// so the client can fire without debouncing.
    /// </summary>
    [HttpPost("recommendations/click")]
    [ProducesResponseType(typeof(LogRecommendationClickResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogRecommendationClick(
        [FromBody] LogRecommendationClickCommand command,
        CancellationToken ct)
    {
        var result = await mediator.Send(command, ct).ConfigureAwait(false);
        return Ok(result);
    }
}
