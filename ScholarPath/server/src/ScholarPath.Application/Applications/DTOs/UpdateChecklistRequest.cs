namespace ScholarPath.Application.Applications.DTOs;

public class UpdateChecklistRequest
{
    public List<ChecklistItem> Items { get; set; } = [];
}

public class ChecklistItem
{
    public string Text { get; set; } = string.Empty;
    public bool IsChecked { get; set; }
}
