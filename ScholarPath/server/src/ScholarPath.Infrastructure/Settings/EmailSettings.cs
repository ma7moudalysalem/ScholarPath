namespace ScholarPath.Infrastructure.Settings;

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string Provider { get; set; } = "none";
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUser { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public string SendGridApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;

    // Keep legacy properties for backward compatibility
    public string SenderEmail
    {
        get => FromEmail;
        set => FromEmail = value;
    }

    public string SenderName
    {
        get => FromName;
        set => FromName = value;
    }

    public string Password
    {
        get => SmtpPassword;
        set => SmtpPassword = value;
    }
}
