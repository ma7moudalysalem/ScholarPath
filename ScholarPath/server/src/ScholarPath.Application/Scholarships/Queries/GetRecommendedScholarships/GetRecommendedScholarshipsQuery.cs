using MediatR;
using ScholarPath.Application.Scholarships.DTOs;

namespace ScholarPath.Application.Scholarships.Queries.GetRecommendedScholarships;

public record GetRecommendedScholarshipsQuery(Guid UserId) : IRequest<RecommendedResponse>;
