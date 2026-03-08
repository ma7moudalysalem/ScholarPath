using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Applications.DTOs;

public class GetApplicationsRequest
{
    public ApplicationStatus? Status { get; set; }
    public string SortBy { get; set; } = "updatedAt";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
