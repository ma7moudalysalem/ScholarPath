using MediatR;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.UpdateExternalStatus;

public record UpdateExternalStatusCommand(Guid Id, ApplicationStatus Status) : IRequest<bool>;
