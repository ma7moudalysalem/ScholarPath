using MediatR;
using ScholarPath.Application.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.Commands.TrackApplication;

public record TrackApplicationCommand(
    Guid ScholarshipId,
    Guid UserId,
    ApplicationStatus? Status,
    string? Notes
) : IRequest<Result<TrackApplicationResponse>>;

public class TrackApplicationResponse
{
    public Guid Id { get; set; }
    public ApplicationStatus Status { get; set; }
    public bool AlreadyExisted { get; set; }
}
