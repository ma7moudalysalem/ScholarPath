using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Ai.Commands.FineTuning;
using ScholarPath.Application.Ai.Commands.ImportExternalScholarships;
using ScholarPath.Application.Ai.Commands.RebuildKnowledgeBase;
using ScholarPath.Application.Ai.DTOs;
using ScholarPath.Application.Ai.Queries.ExportFineTuningDataset;
using ScholarPath.Application.Ai.Queries.FineTuning;
using ScholarPath.Application.Ai.Queries.GetKnowledgeBaseStatus;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Admin endpoints for the AI / RAG pipeline: knowledge-base status and
/// rebuild, dataset import, and fine-tuning job management.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/admin/ai")]
[Produces("application/json")]
public sealed class AdminAiController(IMediator mediator) : ControllerBase
{
    /// <summary>Current RAG knowledge-base index status.</summary>
    [HttpGet("knowledge-base")]
    [ProducesResponseType(typeof(KnowledgeBaseStatusDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> KnowledgeBase(CancellationToken ct)
        => Ok(await mediator.Send(new GetKnowledgeBaseStatusQuery(), ct).ConfigureAwait(false));

    /// <summary>
    /// Rebuilds and re-embeds the knowledge base from the current scholarships
    /// and the FAQ dataset. <paramref name="force"/> re-embeds every document.
    /// </summary>
    [HttpPost("knowledge-base/rebuild")]
    [ProducesResponseType(typeof(KnowledgeBaseRebuildResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> RebuildKnowledgeBase(
        [FromQuery] bool force = false, CancellationToken ct = default)
        => Ok(await mediator.Send(new RebuildKnowledgeBaseCommand(force), ct).ConfigureAwait(false));

    /// <summary>
    /// Imports the bundled external scholarships dataset into the catalogue,
    /// then rebuilds the knowledge base so the new listings become searchable
    /// by the chatbot.
    /// </summary>
    [HttpPost("datasets/import")]
    [ProducesResponseType(typeof(DatasetImportWithRebuildDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ImportDataset(CancellationToken ct)
    {
        var import = await mediator.Send(new ImportExternalScholarshipsCommand(), ct).ConfigureAwait(false);
        var knowledgeBase = await mediator.Send(new RebuildKnowledgeBaseCommand(), ct).ConfigureAwait(false);
        return Ok(new DatasetImportWithRebuildDto(import, knowledgeBase));
    }

    /// <summary>
    /// Exports a supervised fine-tuning dataset (chat JSONL) generated from the
    /// platform's own data — the FAQ knowledge base and the scholarship catalogue.
    /// </summary>
    [HttpGet("fine-tuning/dataset")]
    [ProducesResponseType(typeof(FineTuningDatasetDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> FineTuningDataset(CancellationToken ct)
        => Ok(await mediator.Send(new ExportFineTuningDatasetQuery(), ct).ConfigureAwait(false));

    // ─────────────────────────────────────────────────────────────────────────
    // Fine-tuning job management
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports the JSONL dataset, uploads it to Azure OpenAI, and starts a
    /// supervised fine-tuning job. The job runs asynchronously (30–90 min);
    /// poll <c>GET /api/admin/ai/fine-tuning/status</c> to track progress.
    /// </summary>
    [HttpPost("fine-tuning/start")]
    [ProducesResponseType(typeof(StartFineTuningJobResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> StartFineTuningJob(
        [FromQuery] string baseModel = "gpt-4o-mini", CancellationToken ct = default)
        => Ok(await mediator.Send(new StartFineTuningJobCommand(baseModel), ct).ConfigureAwait(false));

    /// <summary>
    /// Returns the status of the most recently started fine-tuning job,
    /// polling Azure OpenAI for the live state. Also returns the currently
    /// active fine-tuned deployment name (if any).
    /// </summary>
    [HttpGet("fine-tuning/status")]
    [ProducesResponseType(typeof(FineTuningStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> FineTuningStatus(CancellationToken ct)
        => Ok(await mediator.Send(new GetFineTuningJobStatusQuery(), ct).ConfigureAwait(false));

    /// <summary>
    /// Activates a fine-tuned deployment so the chatbot immediately starts using
    /// it without an App Service restart. <paramref name="deploymentName"/> must
    /// match a deployed model in the Azure AI Foundry workspace.
    /// </summary>
    [HttpPut("fine-tuning/activate")]
    [ProducesResponseType(typeof(ActivateFineTunedModelResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ActivateFineTunedModel(
        [FromQuery] string deploymentName, CancellationToken ct)
        => Ok(await mediator.Send(new ActivateFineTunedModelCommand(deploymentName), ct).ConfigureAwait(false));

    /// <summary>
    /// Reverts the chatbot to the base model by clearing the active fine-tuned
    /// deployment name from platform settings.
    /// </summary>
    [HttpDelete("fine-tuning/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeactivateFineTunedModel(CancellationToken ct)
    {
        await mediator.Send(new DeactivateFineTunedModelCommand(), ct).ConfigureAwait(false);
        return NoContent();
    }
}
