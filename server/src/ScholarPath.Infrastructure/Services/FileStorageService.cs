using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Settings;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Real <see cref="IBlobStorageService"/> backed by the configured
/// <c>Storage:Provider</c> (FR-216). <c>Local</c> writes to a folder on disk;
/// <c>AzureBlob</c> writes to an Azure Storage container. Stored objects are
/// addressed by an opaque <c>provider:container/key</c> path so download and
/// delete work for both providers without ambiguity.
/// </summary>
public sealed class FileStorageService(
    IOptions<StorageOptions> options,
    ILogger<FileStorageService> logger) : IBlobStorageService
{
    private readonly StorageOptions _opts = options.Value;

    private bool UseAzure =>
        string.Equals(_opts.Provider, "AzureBlob", StringComparison.OrdinalIgnoreCase);

    public async Task<string> UploadAsync(
        Stream content, string fileName, string contentType, string container, CancellationToken ct)
    {
        // Unique key keeps two same-named uploads from colliding.
        var key = $"{Guid.NewGuid():N}/{fileName}";

        if (UseAzure)
        {
            var blob = ContainerClient(container).GetBlobClient(key);
            await blob.UploadAsync(
                content,
                new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
                ct).ConfigureAwait(false);
            logger.LogInformation("[storage:azure] uploaded {Container}/{Key}", container, key);
            return $"azure:{container}/{key}";
        }

        var root = LocalRoot();
        var dir = Path.Combine(root, container, Path.GetDirectoryName(key)!);
        Directory.CreateDirectory(dir);
        var fullPath = Path.Combine(root, container, key);

        await using (var file = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(file, ct).ConfigureAwait(false);
        }

        logger.LogInformation("[storage:local] uploaded {Container}/{Key}", container, key);
        return $"local:{container}/{key}";
    }

    public async Task<Stream> DownloadAsync(string storagePath, CancellationToken ct)
    {
        var (provider, container, key) = Parse(storagePath);

        if (provider == "azure")
        {
            var blob = ContainerClient(container).GetBlobClient(key);
            var response = await blob.DownloadStreamingAsync(cancellationToken: ct).ConfigureAwait(false);
            return response.Value.Content;
        }

        var fullPath = Path.Combine(LocalRoot(), container, key);
        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Stored file not found.", storagePath);

        return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public async Task DeleteAsync(string storagePath, CancellationToken ct)
    {
        var (provider, container, key) = Parse(storagePath);

        if (provider == "azure")
        {
            var blob = ContainerClient(container).GetBlobClient(key);
            await blob.DeleteIfExistsAsync(cancellationToken: ct).ConfigureAwait(false);
            logger.LogInformation("[storage:azure] deleted {Container}/{Key}", container, key);
            return;
        }

        var fullPath = Path.Combine(LocalRoot(), container, key);
        if (File.Exists(fullPath))
            File.Delete(fullPath);
        logger.LogInformation("[storage:local] deleted {Container}/{Key}", container, key);
    }

    // ─── helpers ────────────────────────────────────────────────────────────

    private string LocalRoot() => Path.GetFullPath(_opts.Local.BasePath);

    private BlobContainerClient ContainerClient(string container)
    {
        var connectionString = _opts.AzureBlob.ConnectionString
            ?? throw new InvalidOperationException("Storage:AzureBlob:ConnectionString is required.");
        // Azure Blob requires lowercase container names — lowercasing is mandated by
        // the service contract, not a localization concern, so CA1308 is suppressed.
#pragma warning disable CA1308
        var client = new BlobContainerClient(connectionString, container.ToLowerInvariant());
#pragma warning restore CA1308
        client.CreateIfNotExists(PublicAccessType.None);
        return client;
    }

    /// <summary>Splits a <c>provider:container/key</c> storage path into its parts.</summary>
    private (string Provider, string Container, string Key) Parse(string storagePath)
    {
        var colon = storagePath.IndexOf(':', StringComparison.Ordinal);
        if (colon <= 0)
        {
            // Legacy / unprefixed path — assume the configured provider, treat the
            // whole value as "container/key".
            var slash0 = storagePath.IndexOf('/', StringComparison.Ordinal);
            return slash0 <= 0
                ? throw new InvalidOperationException($"Malformed storage path '{storagePath}'.")
                : (UseAzure ? "azure" : "local", storagePath[..slash0], storagePath[(slash0 + 1)..]);
        }

        var provider = storagePath[..colon];
        var rest = storagePath[(colon + 1)..];
        var slash = rest.IndexOf('/', StringComparison.Ordinal);
        return slash <= 0
            ? throw new InvalidOperationException($"Malformed storage path '{storagePath}'.")
            : (provider, rest[..slash], rest[(slash + 1)..]);
    }
}
