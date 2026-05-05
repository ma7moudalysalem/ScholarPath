using MediatR;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Scholarships.Commands.UnsaveScholarship;

public record UnsaveScholarshipCommand(Guid ScholarshipId, Guid UserId) : IRequest<Result<bool>>;
