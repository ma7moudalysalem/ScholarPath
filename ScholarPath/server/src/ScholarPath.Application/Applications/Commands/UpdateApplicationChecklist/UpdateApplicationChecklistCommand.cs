using MediatR;
using ScholarPath.Application.Applications.DTOs;
using ScholarPath.Application.Common;

namespace ScholarPath.Application.Applications.Commands.UpdateApplicationChecklist;

public record UpdateApplicationChecklistCommand(
    Guid Id,
    Guid UserId,
    List<ChecklistItem> Items
) : IRequest<Result<UpdateApplicationChecklistResponse>>;

public class UpdateApplicationChecklistResponse
{
    public DateTime UpdatedAt { get; set; }
}
