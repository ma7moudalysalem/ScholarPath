using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/files")]
[Authorize] 
public class FilesController : BaseController
{
    private readonly IWebHostEnvironment _environment;

    public FilesController(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpPost("upload-proof")]
    public async Task<IActionResult> UploadProof(IFormFile file)
    {
        // validation for file upload 
        var allowedTypes = new[] { "application/pdf", "image/jpeg", "image/png" };
        if (!allowedTypes.Contains(file.ContentType))
            return BadRequestResult("errors.files.invalidType");

        // validation for file size(5MB limit)
        if (file.Length > 5 * 1024 * 1024)
            return BadRequestResult("errors.files.fileTooLarge");

        // Storage
        var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "upgrade-requests");
        if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

        // Sanitization
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { filePath = $"/uploads/upgrade-requests/{fileName}" });
    }
}
