using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;
using ScholarPath.Domain.Interfaces;

namespace ScholarPath.Application.Scholarships.Queries.GetRecommendedScholarships;

public class GetRecommendedScholarshipsQueryHandler
    : IRequestHandler<GetRecommendedScholarshipsQuery, RecommendedResponse>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly ICachingService _cachingService;

    public GetRecommendedScholarshipsQueryHandler(
        IApplicationDbContext dbContext,
        ICachingService cachingService)
    {
        _dbContext = dbContext;
        _cachingService = cachingService;
    }

    public async Task<RecommendedResponse> Handle(
        GetRecommendedScholarshipsQuery request, CancellationToken cancellationToken)
    {
        // Check cache first
        var cacheKey = $"recommendations:{request.UserId}";
        var cached = await _cachingService.GetAsync<RecommendedResponse>(cacheKey, cancellationToken);
        if (cached is not null)
            return cached;

        // Load user profile
        var profile = await _dbContext.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, cancellationToken);

        if (profile is null || profile.FieldOfStudy is null)
        {
            return new RecommendedResponse
            {
                Items = [],
                ProfileIncomplete = true
            };
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
        var scored = new List<(Domain.Entities.Scholarship Scholarship, int Score, List<string> Reasons)>();

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

        return result;
    }
}
