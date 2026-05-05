using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Application.Scholarships.DTOs;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Scholarships.Queries.SearchScholarships;

public record SearchScholarshipsQuery(
    string? Search,
    string? Country,
    DegreeLevel? DegreeLevel,
    string? FieldOfStudy,
    ScholarshipFundingType? FundingType,
    DateTime? DeadlineFrom,
    DateTime? DeadlineTo,
    int Page,
    int PageSize,
    ScholarshipSortBy SortBy,
    bool IncludeExpired,
    Guid? CurrentUserId
) : IRequest<PaginatedResponse<ScholarshipListItemDto>>;
