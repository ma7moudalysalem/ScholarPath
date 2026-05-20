namespace ScholarPath.Infrastructure.Settings;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "https://scholarpath.local";
    public string Audience { get; set; } = "https://scholarpath.local";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
    public int RefreshTokenRememberMeExpirationDays { get; set; } = 30;

    // ─── RS256 asymmetric signing ────────────────────────────────────────────
    // Tokens are signed with an RSA private key and validated with the matching
    // public key. The key itself is supplied by an IJwtKeyProvider, never stored
    // here in plain config.

    /// <summary>
    /// Azure Key Vault base URI (e.g. https://scholarpath-kv.vault.azure.net/).
    /// When set, the production <c>KeyVaultJwtKeyProvider</c> is selected and the
    /// RSA key is fetched from the vault via <c>DefaultAzureCredential</c>.
    /// Leave empty in development to use the local key provider.
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>Name of the RSA key inside Key Vault. Used only when <see cref="KeyVaultUri"/> is set.</summary>
    public string KeyName { get; set; } = "scholarpath-jwt-signing";

    /// <summary>
    /// Filesystem path to an RSA private-key PEM used by the development key
    /// provider. If the file is missing an ephemeral RSA-2048 key is generated
    /// at startup. Never commit a real private key to source control.
    /// </summary>
    public string? DevKeyPath { get; set; }
}

/// <summary>
/// Application-level encryption of sensitive database columns at rest (SRS
/// security NFR). The key itself is supplied by an <c>IFieldEncryptionKeyProvider</c>,
/// never stored here in plain config: Key Vault when <see cref="KeyVaultUri"/> is
/// set (production), the Base64 <see cref="DevKey"/> otherwise (development).
/// </summary>
public sealed class FieldEncryptionOptions
{
    public const string SectionName = "FieldEncryption";

    /// <summary>
    /// Azure Key Vault base URI (e.g. https://scholarpath-kv.vault.azure.net/).
    /// When set, the production <c>KeyVaultFieldEncryptionKeyProvider</c> is
    /// selected and the AES key is read from the vault via
    /// <c>DefaultAzureCredential</c>. Leave empty in development to use the local
    /// key provider.
    /// </summary>
    public string? KeyVaultUri { get; set; }

    /// <summary>
    /// Name of the Key Vault <b>secret</b> holding the Base64-encoded 256-bit AES
    /// key. Used only when <see cref="KeyVaultUri"/> is set.
    /// </summary>
    public string KeyName { get; set; } = "field-encryption-key";

    /// <summary>
    /// Base64-encoded 256-bit AES key used by the development key provider. Field
    /// encryption needs a key that is stable across restarts, so this is a fixed
    /// configured value — never an ephemeral key. A real key must never be
    /// committed; production reads it from Key Vault instead.
    /// </summary>
    public string? DevKey { get; set; }
}

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    /// <summary>
    /// When true, the deterministic <c>StubSsoService</c> is registered instead
    /// of the real provider — used for local dev and tests where no real OAuth
    /// credentials exist. Defaults to false: the real <c>SsoService</c> is used.
    /// </summary>
    public bool UseStub { get; set; }

    public ExternalAuthProviderOptions Google { get; set; } = new();
    public ExternalAuthProviderOptions Microsoft { get; set; } = new();
}

public sealed class ExternalAuthProviderOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Optional fixed OAuth redirect URI. When empty the redirect URI supplied
    /// by the caller (the SPA callback URL) is used for both the authorize step
    /// and the token exchange — they must match exactly.
    /// </summary>
    public string? RedirectUri { get; set; }
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

    /// <summary>RAG: how many knowledge-base documents to retrieve as grounding context per chat turn.</summary>
    public int RagTopK { get; set; } = 4;

    /// <summary>RAG: minimum cosine similarity for a retrieved document to be treated as relevant context.</summary>
    public double RagMinScore { get; set; } = 0.15;

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

    /// <summary>Chat-completions deployment name.</summary>
    public string DeploymentName { get; set; } = "gpt-4o-mini";

    /// <summary>Embeddings deployment name — produces the RAG knowledge-base vectors.</summary>
    public string EmbeddingDeploymentName { get; set; } = "text-embedding-3-small";

    /// <summary>Dimensionality of the embeddings deployment (text-embedding-3-small = 1536).</summary>
    public int EmbeddingDimensions { get; set; } = 1536;

    /// <summary>Azure OpenAI REST API version.</summary>
    public string ApiVersion { get; set; } = "2024-10-21";

    /// <summary>
    /// Optional deployment name of a fine-tuned chat model. When set, chat uses
    /// it instead of <see cref="DeploymentName"/> — see the fine-tuning runbook.
    /// </summary>
    public string? FineTunedDeploymentName { get; set; }
}

/// <summary>
/// Azure Event Hubs configuration for real-time domain-event streaming (PB-018).
/// When <see cref="ConnectionString"/> is provided, the real
/// <c>EventHubPublisher</c> is registered; otherwise a no-op stub is used so
/// dev/test environments never fail due to missing Event Hub credentials.
/// </summary>
public sealed class EventHubOptions
{
    public const string SectionName = "EventHub";

    /// <summary>
    /// Event Hubs namespace connection string. When absent the stub publisher
    /// is used and domain events are only logged — no Event Hub calls are made.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>Name of the event hub within the namespace. Defaults to "domain-events".</summary>
    public string HubName { get; set; } = "domain-events";
}

/// <summary>
/// Antivirus scanning of uploaded files (SRS security NFR). When
/// <see cref="Enabled"/> is true the real <c>ClamAvFileScanService</c> is
/// registered and every upload is scanned against a <c>clamd</c> daemon before
/// the bytes are stored; if the daemon is unreachable the upload is rejected
/// (fail-closed). When false the <c>NoOpFileScanService</c> is used — for dev
/// and tests where no ClamAV daemon runs.
/// </summary>
public sealed class FileScanningOptions
{
    public const string SectionName = "FileScanning";

    /// <summary>Master switch. Off by default so nothing breaks without a ClamAV daemon.</summary>
    public bool Enabled { get; set; }

    /// <summary>Hostname of the <c>clamd</c> daemon (the ClamAV container / sidecar).</summary>
    public string ClamAvHost { get; set; } = "localhost";

    /// <summary>TCP port <c>clamd</c> listens on. ClamAV's default INSTREAM port is 3310.</summary>
    public int ClamAvPort { get; set; } = 3310;
}
