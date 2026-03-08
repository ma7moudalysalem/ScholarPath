using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Application.Scholarships.DTOs;

namespace ScholarPath.Application.Scholarships.Queries.GetSavedScholarships;

public record GetSavedScholarshipsQuery(
    Guid UserId,
    int Page,
    int PageSize
) : IRequest<PaginatedResponse<ScholarshipListItemDto>>;
