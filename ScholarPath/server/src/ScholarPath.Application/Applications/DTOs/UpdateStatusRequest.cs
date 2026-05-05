using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.DTOs;

public class UpdateStatusRequest
{
    public ApplicationStatus Status { get; set; }
}
