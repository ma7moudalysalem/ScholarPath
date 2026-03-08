using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ScholarPath.Application.Scholarships.Commands.SaveScholarship;
using ScholarPath.Application.Scholarships.Commands.UnsaveScholarship;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Application.Scholarships.Queries.GetRecommendedScholarships;
using ScholarPath.Application.Scholarships.Queries.GetSavedScholarships;
using ScholarPath.Application.Scholarships.Queries.SearchScholarships;
using ScholarPath.Domain.Entities;

namespace ScholarPath.API.Controllers;

public class ScholarshipsController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<ScholarshipSearchRequest> _searchValidator;

    public ScholarshipsController(
        UserManager<ApplicationUser> userManager,
        IValidator<ScholarshipSearchRequest> searchValidator)
    {
        _userManager = userManager;
        _searchValidator = searchValidator;
    }

    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] ScholarshipSearchRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _searchValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return BadRequestResult(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        Guid? userId = User.Identity?.IsAuthenticated == true
            ? (await _userManager.GetUserAsync(User))?.Id
            : null;

        var result = await Mediator.Send(new SearchScholarshipsQuery(
            request.Search,
            request.Country,
            request.DegreeLevel,
            request.FieldOfStudy,
            request.FundingType,
            request.DeadlineFrom,
            request.DeadlineTo,
            request.Page,
            request.PageSize,
            request.SortBy,
            request.IncludeExpired,
            userId
        ), cancellationToken);

        return Ok(result);
    }

    [HttpGet("recommended")]
    [Authorize]
    public async Task<IActionResult> GetRecommended(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var result = await Mediator.Send(
            new GetRecommendedScholarshipsQuery(user.Id), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> SaveScholarship(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var result = await Mediator.Send(
            new SaveScholarshipCommand(id, user.Id), cancellationToken);

        if (!result.IsSuccess)
            return NotFoundResult(result.Error!);

        return Ok();
    }

    [HttpDelete("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> UnsaveScholarship(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        await Mediator.Send(
            new UnsaveScholarshipCommand(id, user.Id), cancellationToken);

        return Ok();
    }

    [HttpGet("/api/v{version:apiVersion}/saved-scholarships")]
    [Authorize]
    public async Task<IActionResult> GetSavedScholarships(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var result = await Mediator.Send(
            new GetSavedScholarshipsQuery(user.Id, page, pageSize), cancellationToken);

        return Ok(result);
    }
}
