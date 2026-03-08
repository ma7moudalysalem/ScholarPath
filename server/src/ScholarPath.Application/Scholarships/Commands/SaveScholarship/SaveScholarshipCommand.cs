using MediatR;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Scholarships.Commands.SaveScholarship;

public record SaveScholarshipCommand(Guid ScholarshipId, Guid UserId) : IRequest<Result<bool>>;
