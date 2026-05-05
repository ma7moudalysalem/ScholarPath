namespace ScholarPath.Application.Common.Models;

public class AppSettings
{
    public const string SectionName = "App";
    public string ClientUrl { get; set; } = "http://localhost:3000";
}

