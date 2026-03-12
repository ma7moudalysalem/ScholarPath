using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.Files.Commands.UploadProofDocument;
using Microsoft.AspNetCore.Http;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/files")]
[Authorize]
public class FilesController : BaseController
{
    public FilesController()
    {
    }

    [HttpPost("upload")]
    [RequestSizeLimit(30_000_000)] // 30 MB total limit
    public async Task<IActionResult> Upload(
        [FromForm] List<IFormFile> files,
        [FromForm] Guid? upgradeRequestId,
        CancellationToken cancellationToken)
    {
        try
        {
            var fileDtos = files.Select(f => new FileDto(f.FileName, f.ContentType, f.Length, f.OpenReadStream())).ToList();
            var command = new UploadProofDocumentCommand(fileDtos, upgradeRequestId);
            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFoundResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequestResult(ex.Message);
        }
    }
}
