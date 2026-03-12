using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.UpgradeRequests.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitConsultantUpgrade;
using ScholarPath.Application.UpgradeRequests.Commands.SubmitCompanyUpgrade;
using ScholarPath.Application.UpgradeRequests.Queries.GetMyUpgradeRequest;
using ScholarPath.Application.UpgradeRequests.Commands.ResubmitUpgradeRequest;

namespace ScholarPath.API.Controllers;

[Route("api/v{version:apiVersion}/upgrade-requests")]
[Authorize]
public class UpgradeRequestsController : BaseController
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _dbContext;
    private readonly IValidator<ConsultantUpgradeRequest> _consultantValidator;
    private readonly IValidator<CompanyUpgradeRequest> _companyValidator;

    public UpgradeRequestsController(
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext dbContext,
        IValidator<ConsultantUpgradeRequest> consultantValidator,
        IValidator<CompanyUpgradeRequest> companyValidator)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _consultantValidator = consultantValidator;
        _companyValidator = companyValidator;
    }

    [HttpPost("consultant")]
    public async Task<IActionResult> SubmitConsultant(
        [FromBody] ConsultantUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _consultantValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequestResult(validationResult.Errors.Select(e => e.ErrorMessage));

        try
        {
            var command = new SubmitConsultantUpgradeCommand(
                request.ExperienceSummary,
                request.Languages,
                request.Education,
                request.ExpertiseTags,
                request.Links);

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResult(ex.Message);
        }
    }

    [HttpPost("company")]
    public async Task<IActionResult> SubmitCompany(
        [FromBody] CompanyUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        var validationResult = await _companyValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
            return BadRequestResult(validationResult.Errors.Select(e => e.ErrorMessage));

        try
        {
            var command = new SubmitCompanyUpgradeCommand(
                request.CompanyName,
                request.Country,
                request.Website,
                request.ContactPersonName,
                request.ContactEmail,
                request.ContactPhone,
                request.CompanyRegistrationNumber);

            var result = await Mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ConflictResult(ex.Message);
        }
    }

    [HttpGet("my-status")]
    public async Task<IActionResult> GetMyStatus(CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetMyUpgradeRequestQuery();
            var result = await Mediator.Send(query, cancellationToken);
            
            if (result is null) return Ok(new { });
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return UnauthorizedResult(ex.Message);
        }
    }

    [HttpPut("{id:guid}/resubmit")]
    public async Task<IActionResult> Resubmit(
        Guid id,
        [FromBody] ResubmitUpgradeRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = new ResubmitUpgradeRequestCommand(
                id,
                request.ExperienceSummary,
                request.Languages,
                request.Education,
                request.ExpertiseTags,
                request.Links,
                request.CompanyName,
                request.Country,
                request.Website,
                request.ContactPersonName,
                request.ContactEmail,
                request.ContactPhone,
                request.CompanyRegistrationNumber
            );

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

    private async Task<List<ExpertiseTag>> ResolveExpertiseTagsAsync(
        List<string> tagNames, CancellationToken cancellationToken)
    {
        var result = new List<ExpertiseTag>();
        foreach (var name in tagNames.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var existing = await _dbContext.ExpertiseTags
                .FirstOrDefaultAsync(t => t.Name == name, cancellationToken);
            result.Add(existing ?? new ExpertiseTag { Name = name });
        }
        return result;
    }
}
