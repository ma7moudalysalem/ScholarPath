using MediatR;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationNotes;

public record UpdateApplicationNotesCommand(
    Guid Id,
    Guid UserId,
    string? Notes
) : IRequest<Result<UpdateApplicationNotesResponse>>;

public class UpdateApplicationNotesResponse
{
    public DateTime UpdatedAt { get; set; }
}
