using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

public class ScholarshipsController : BaseController
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<ScholarshipSearchRequest> _searchValidator;

    public ScholarshipsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IValidator<ScholarshipSearchRequest> searchValidator)
    {
        _dbContext = dbContext;
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

        var query = _dbContext.Scholarships.AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Published);

        // Exclude expired unless requested
        if (!request.IncludeExpired)
            query = query.Where(s => s.Deadline == null || s.Deadline >= DateTime.UtcNow.Date);

        // Text search
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLower();
            query = query.Where(s => s.Title.ToLower().Contains(search)
                || (s.ProviderName != null && s.ProviderName.ToLower().Contains(search))
                || (s.Description != null && s.Description.ToLower().Contains(search)));
        }

        // Filters
        if (!string.IsNullOrWhiteSpace(request.Country))
            query = query.Where(s => s.Country == request.Country);
        if (request.DegreeLevel.HasValue)
            query = query.Where(s => s.DegreeLevel == request.DegreeLevel.Value);
        if (!string.IsNullOrWhiteSpace(request.FieldOfStudy))
            query = query.Where(s => s.FieldOfStudy == request.FieldOfStudy);
        if (request.FundingType.HasValue)
            query = query.Where(s => s.FundingType == request.FundingType.Value);
        if (request.DeadlineFrom.HasValue)
            query = query.Where(s => s.Deadline >= request.DeadlineFrom.Value);
        if (request.DeadlineTo.HasValue)
            query = query.Where(s => s.Deadline <= request.DeadlineTo.Value);

        // Sorting
        query = request.SortBy switch
        {
            ScholarshipSortBy.DeadlineSoonest => query.OrderBy(s => s.Deadline),
            ScholarshipSortBy.Newest => query.OrderByDescending(s => s.CreatedAt),
            ScholarshipSortBy.HighestFunding => query.OrderByDescending(s => s.AwardAmount),
            _ => query.OrderByDescending(s => s.CreatedAt) // Relevance default
        };

        // Pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Determine saved scholarship IDs for authenticated users
        var savedScholarshipIds = new HashSet<Guid>();
        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is not null)
            {
                savedScholarshipIds = (await _dbContext.SavedScholarships
                    .AsNoTracking()
                    .Where(ss => ss.UserId == user.Id)
                    .Select(ss => ss.ScholarshipId)
                    .ToListAsync(cancellationToken))
                    .ToHashSet();
            }
        }

        var today = DateTime.UtcNow.Date;

        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(s => new ScholarshipListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                TitleAr = s.TitleAr,
                ProviderName = s.ProviderName,
                ProviderNameAr = s.ProviderNameAr,
                Country = s.Country,
                DegreeLevel = s.DegreeLevel,
                FundingType = s.FundingType,
                AwardAmount = s.AwardAmount,
                Currency = s.Currency,
                Deadline = s.Deadline,
                ImageUrl = s.ImageUrl,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync(cancellationToken);

        // Compute derived fields in memory
        foreach (var item in items)
        {
            if (item.Deadline.HasValue)
            {
                item.DeadlineCountdownDays = (item.Deadline.Value.Date - today).Days;
                item.IsExpiringSoon = item.DeadlineCountdownDays is > 0 and <= 7;
            }

            item.IsSaved = savedScholarshipIds.Contains(item.Id);
        }

        var response = new PaginatedResponse<ScholarshipListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };

        return Ok(response);
    }
}
