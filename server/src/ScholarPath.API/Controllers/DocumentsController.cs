using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Documents;
using ScholarPath.Application.Documents.Commands.DeleteDocument;
using ScholarPath.Application.Documents.Commands.UploadDocument;
using ScholarPath.Application.Documents.Queries.DownloadDocument;
using ScholarPath.Application.Documents.Queries.GetMyDocuments;
using ScholarPath.Domain.Enums;

namespace ScholarPath.API.Controllers;

/// <summary>
/// Personal document vault (FR-216). Authenticated users upload, list, download,
/// and delete their own documents; admins may download or delete any document.
/// </summary>
[ApiController]
[Route("api/documents")]
[Authorize]
[Produces("application/json")]
public sealed class DocumentsController(IMediator mediator) : ControllerBase
{
    /// <summary>Uploads a file to the caller's document vault.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(DocumentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [RequestSizeLimit(26 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        IFormFile file,
        [FromForm] DocumentCategory category = DocumentCategory.Other,
        [FromForm] Guid? applicationTrackerId = null,
        [FromForm] OnboardingDocumentType? onboardingType = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No file uploaded.");

        await using var stream = file.OpenReadStream();
        var result = await mediator.Send(new UploadDocumentCommand(
            stream, file.FileName, file.ContentType, file.Length, category, applicationTrackerId, onboardingType), ct);

        return CreatedAtAction(nameof(Download), new { id = result.Id }, result);
    }

    /// <summary>Lists the caller's own vault documents, optionally filtered by category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<DocumentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] DocumentCategory? category, CancellationToken ct)
        => Ok(await mediator.Send(new GetMyDocumentsQuery(category), ct));

    /// <summary>Streams a document's bytes. Owner-only (admins may download any).</summary>
    [HttpGet("{id:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await mediator.Send(new DownloadDocumentQuery(id), ct);
        return File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>Deletes a document. Owner-only (admins may delete any).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await mediator.Send(new DeleteDocumentCommand(id), ct);
        return NoContent();
    }
}
