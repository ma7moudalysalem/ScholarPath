using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Application.Scholarships.DTOs;

namespace ScholarPath.Application.Scholarships.Queries.GetScholarshipDetail;

public record GetScholarshipDetailQuery(Guid ScholarshipId, Guid? CurrentUserId)
    : IRequest<Result<ScholarshipDetailDto>>;
