namespace ScholarPath.Application.Applications.DTOs;

public class UpdateRemindersRequest
{
    public int[] Presets { get; set; } = [];
    public ReminderChannels Channels { get; set; } = new();
    public bool Paused { get; set; }
}

public class ReminderChannels
{
    public bool InApp { get; set; } = true;
    public bool Email { get; set; }
}
