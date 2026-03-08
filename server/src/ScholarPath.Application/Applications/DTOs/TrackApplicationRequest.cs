using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.DTOs;

public class TrackApplicationRequest
{
    public Guid ScholarshipId { get; set; }
    public ApplicationStatus? Status { get; set; }
    public string? Notes { get; set; }
}
