using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Entities;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.API.Controllers;

public class ScholarshipsController : BaseController
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IValidator<ScholarshipSearchRequest> _searchValidator;
    private readonly ICachingService _cachingService;

    public ScholarshipsController(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IValidator<ScholarshipSearchRequest> searchValidator,
        ICachingService cachingService)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _searchValidator = searchValidator;
        _cachingService = cachingService;
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

    [HttpGet("recommended")]
    [Authorize]
    public async Task<IActionResult> GetRecommended(CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        // Check cache first
        var cacheKey = $"recommendations:{user.Id}";
        var cached = await _cachingService.GetAsync<RecommendedResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return Ok(cached);

        // Load user profile
        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == user.Id, cancellationToken);

        if (profile is null || profile.FieldOfStudy is null)
        {
            var incompleteResponse = new RecommendedResponse
            {
                Items = [],
                ProfileIncomplete = true
            };
            return Ok(incompleteResponse);
        }

        // Load active, non-expired, published scholarships
        var today = DateTime.UtcNow.Date;
        var scholarships = await _dbContext.Scholarships
            .AsNoTracking()
            .Where(s => s.Status == ScholarshipStatus.Published
                        && s.IsActive
                        && (s.Deadline == null || s.Deadline >= today))
            .ToListAsync(cancellationToken);

        // Parse profile interests (JSON array)
        var profileInterests = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(profile.Interests))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(profile.Interests);
                if (parsed is not null)
                {
                    foreach (var tag in parsed)
                        profileInterests.Add(tag);
                }
            }
            catch (JsonException)
            {
                // Ignore malformed JSON
            }
        }

        var profileFieldOfStudy = profile.FieldOfStudy.Trim();

        // Score each scholarship
        var scored = new List<(Scholarship Scholarship, int Score, List<string> Reasons)>();

        foreach (var s in scholarships)
        {
            var score = 0;
            var reasons = new List<string>();

            // FieldOfStudy match (+3): case-insensitive contains
            if (!string.IsNullOrWhiteSpace(s.FieldOfStudy) &&
                s.FieldOfStudy.Contains(profileFieldOfStudy, StringComparison.OrdinalIgnoreCase))
            {
                score += 3;
                reasons.Add("Field match");
            }

            // Country match (+2): profile.Country or profile.TargetCountry matches scholarship.Country
            if (!string.IsNullOrWhiteSpace(s.Country))
            {
                var countryMatch = false;
                if (!string.IsNullOrWhiteSpace(profile.Country) &&
                    string.Equals(s.Country, profile.Country, StringComparison.OrdinalIgnoreCase))
                {
                    countryMatch = true;
                }
                if (!string.IsNullOrWhiteSpace(profile.TargetCountry) &&
                    string.Equals(s.Country, profile.TargetCountry, StringComparison.OrdinalIgnoreCase))
                {
                    countryMatch = true;
                }

                if (countryMatch)
                {
                    score += 2;
                    reasons.Add("Country match");
                }
            }

            // Tag overlap (+1 per match)
            if (!string.IsNullOrWhiteSpace(s.Tags) && profileInterests.Count > 0)
            {
                try
                {
                    var scholarshipTags = JsonSerializer.Deserialize<string[]>(s.Tags);
                    if (scholarshipTags is not null)
                    {
                        foreach (var tag in scholarshipTags)
                        {
                            if (profileInterests.Contains(tag))
                            {
                                score += 1;
                                reasons.Add($"Tag: {tag}");
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Ignore malformed JSON
                }
            }

            if (score > 0)
                scored.Add((s, score, reasons));
        }

        // Sort by score descending, take top 10
        var top = scored
            .OrderByDescending(x => x.Score)
            .Take(10)
            .ToList();

        var recommendedItems = top.Select(x =>
        {
            var dto = new RecommendedScholarshipDto
            {
                Id = x.Scholarship.Id,
                Title = x.Scholarship.Title,
                TitleAr = x.Scholarship.TitleAr,
                ProviderName = x.Scholarship.ProviderName,
                ProviderNameAr = x.Scholarship.ProviderNameAr,
                Country = x.Scholarship.Country,
                DegreeLevel = x.Scholarship.DegreeLevel,
                FundingType = x.Scholarship.FundingType,
                AwardAmount = x.Scholarship.AwardAmount,
                Currency = x.Scholarship.Currency,
                Deadline = x.Scholarship.Deadline,
                ImageUrl = x.Scholarship.ImageUrl,
                Score = x.Score,
                MatchReasons = x.Reasons.ToArray()
            };

            if (dto.Deadline.HasValue)
            {
                dto.DeadlineCountdownDays = (dto.Deadline.Value.Date - today).Days;
                dto.IsExpiringSoon = dto.DeadlineCountdownDays is > 0 and <= 7;
            }

            return dto;
        }).ToList();

        var result = new RecommendedResponse
        {
            Items = recommendedItems,
            ProfileIncomplete = false
        };

        // Cache for 5 minutes
        await _cachingService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Ok(result);
    }

    [HttpPost("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> SaveScholarship(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var scholarshipExists = await _dbContext.Scholarships
            .AnyAsync(s => s.Id == id, cancellationToken);
        if (!scholarshipExists)
            return NotFoundResult("Scholarship not found.");

        var alreadySaved = await _dbContext.SavedScholarships
            .AnyAsync(ss => ss.UserId == user.Id && ss.ScholarshipId == id, cancellationToken);

        if (!alreadySaved)
        {
            var savedScholarship = new SavedScholarship
            {
                UserId = user.Id,
                ScholarshipId = id
            };
            _dbContext.SavedScholarships.Add(savedScholarship);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Ok();
    }

    [HttpDelete("{id:guid}/save")]
    [Authorize]
    public async Task<IActionResult> UnsaveScholarship(Guid id, CancellationToken cancellationToken)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Unauthorized();

        var saved = await _dbContext.SavedScholarships
            .FirstOrDefaultAsync(ss => ss.UserId == user.Id && ss.ScholarshipId == id, cancellationToken);

        if (saved is not null)
        {
            _dbContext.SavedScholarships.Remove(saved);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

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

        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 1;
        if (pageSize > 100) pageSize = 100;

        var query = _dbContext.SavedScholarships
            .AsNoTracking()
            .Where(ss => ss.UserId == user.Id)
            .Join(
                _dbContext.Scholarships.AsNoTracking()
                    .Where(s => s.Status == ScholarshipStatus.Published && s.IsActive),
                ss => ss.ScholarshipId,
                s => s.Id,
                (ss, s) => s);

        var totalCount = await query.CountAsync(cancellationToken);

        var today = DateTime.UtcNow.Date;

        var items = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        foreach (var item in items)
        {
            if (item.Deadline.HasValue)
            {
                item.DeadlineCountdownDays = (item.Deadline.Value.Date - today).Days;
                item.IsExpiringSoon = item.DeadlineCountdownDays is > 0 and <= 7;
            }

            item.IsSaved = true;
        }

        var response = new PaginatedResponse<ScholarshipListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }
}
