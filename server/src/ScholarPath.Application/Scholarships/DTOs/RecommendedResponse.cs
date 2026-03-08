namespace ScholarPath.Application.Scholarships.DTOs;

public class RecommendedResponse
{
    public List<RecommendedScholarshipDto> Items { get; set; } = [];
    public bool ProfileIncomplete { get; set; }
}
