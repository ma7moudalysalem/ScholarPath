namespace ScholarPath.Infrastructure.Settings;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "https://scholarpath.local";
    public string Audience { get; set; } = "https://scholarpath.local";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int RefreshTokenRememberMeExpirationDays { get; set; } = 30;
    public string SigningKey { get; set; } = default!; // dev only — use RS256 + Key Vault in prod
}

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";
    public string? SecretKey { get; set; }
    public string? PublishableKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string? ConnectClientId { get; set; }
}

public sealed class EmailOptions
{
    public const string SectionName = "Email";
    public string Provider { get; set; } = "MailKit"; // MailKit | SendGrid | None
    public string From { get; set; } = "no-reply@scholarpath.local";
    public string FromName { get; set; } = "ScholarPath";
    public MailKitEmailOptions MailKit { get; set; } = new();
    public SendGridEmailOptions SendGrid { get; set; } = new();
}

public sealed class MailKitEmailOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1025;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; }
}

public sealed class SendGridEmailOptions
{
    public string? ApiKey { get; set; }
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";
    public bool Enabled { get; set; }
    public string ConnectionString { get; set; } = "localhost:6379";
}

public sealed class HangfireOptions
{
    public const string SectionName = "Hangfire";
    public bool Enabled { get; set; }
    public bool DashboardEnabled { get; set; } = true;
}

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "Local";
    public LocalStorageOptions Local { get; set; } = new();
    public AzureBlobStorageOptions AzureBlob { get; set; } = new();
}

public sealed class LocalStorageOptions
{
    public string BasePath { get; set; } = "./uploads";
}

public sealed class AzureBlobStorageOptions
{
    public string? ConnectionString { get; set; }
    public string ContainerName { get; set; } = "scholarpath-uploads";
}

public sealed class AiOptions
{
    public const string SectionName = "Ai";
    public string Provider { get; set; } = "Stub";

    /// <summary>How many recommendations to return per call. Stub scores the whole open catalog then trims to this.</summary>
    public int RecommendationTopN { get; set; } = 5;

    /// <summary>Per-user rolling 24h cost ceiling. The command layer refuses AI calls that would push a user over this.</summary>
    public decimal DailyUserCostLimitUsd { get; set; } = 1.00m;

    public OpenAiOptions OpenAi { get; set; } = new();
    public AzureOpenAiOptions AzureOpenAi { get; set; } = new();
}

public sealed class OpenAiOptions
{
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-4o-mini";
}

public sealed class AzureOpenAiOptions
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
    public string DeploymentName { get; set; } = "gpt-4o-mini";
}
