using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Scholarships.Commands.ConfigureReviewFee;

namespace ScholarPath.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScholarshipsController(IMediator mediator) : ControllerBase
{
    [HttpPost("{id:guid}/review-fee")]
    [Authorize(Roles = "Company,Admin")]
    public async Task<IActionResult> ConfigureReviewFee(Guid id, [FromBody] ConfigureReviewFeeRequest request, CancellationToken ct)
    {
        var command = new ConfigureReviewFeeCommand(id, request.ReviewFeeUsd);
        var result = await mediator.Send(command, ct);
        return result ? Ok() : BadRequest();
    }
}

public record ConfigureReviewFeeRequest(decimal ReviewFeeUsd);
