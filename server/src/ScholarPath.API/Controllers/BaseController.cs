using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ScholarPath.API.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public abstract class BaseController : ControllerBase
{
    private IMediator? _mediator;

    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    protected ActionResult<T> OkResult<T>(T value)
    {
        return Ok(value);
    }

    protected ActionResult BadRequestResult(string error)
    {
        return BadRequest(new { Error = error });
    }

    protected ActionResult BadRequestResult(IEnumerable<string> errors)
    {
        return BadRequest(new { Errors = errors });
    }

    protected ActionResult NotFoundResult(string message)
    {
        return NotFound(new { Error = message });
    }

    protected ActionResult UnauthorizedResult(string message)
    {
        return Unauthorized(new { Error = message });
    }

    protected ActionResult ForbiddenResult(string message)
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { Error = message });
    }
}
