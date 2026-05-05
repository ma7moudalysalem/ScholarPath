using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationStatus;

public record UpdateApplicationStatusCommand(
    Guid Id,
    Guid UserId,
    ApplicationStatus Status
) : IRequest<Result<UpdateApplicationStatusResponse>>;

public class UpdateApplicationStatusResponse
{
    public DateTime UpdatedAt { get; set; }
}
