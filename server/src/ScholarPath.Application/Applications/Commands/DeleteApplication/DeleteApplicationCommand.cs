using MediatR;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Applications.Commands.DeleteApplication;

public record DeleteApplicationCommand(
    Guid Id,
    Guid UserId
) : IRequest<Result<bool>>;
