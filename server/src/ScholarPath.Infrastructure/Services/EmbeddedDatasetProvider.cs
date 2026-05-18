using System.Reflection;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Reads the curated datasets that are embedded into the Infrastructure
/// assembly at build time (see the "RAG datasets" item group in
/// ScholarPath.Infrastructure.csproj). Embedding the files keeps them
/// available no matter how the app is published or deployed.
/// </summary>
public sealed class EmbeddedDatasetProvider : IDatasetProvider
{
    private const string Prefix = "ScholarPath.Datasets.";
    private const string Suffix = ".json";

    private static readonly Assembly Assembly = typeof(EmbeddedDatasetProvider).Assembly;

    public IReadOnlyList<string> AvailableDatasets =>
    [
        .. Assembly.GetManifestResourceNames()
            .Where(n => n.StartsWith(Prefix, StringComparison.Ordinal)
                     && n.EndsWith(Suffix, StringComparison.OrdinalIgnoreCase))
            .Select(n => n[Prefix.Length..^Suffix.Length])
            .OrderBy(n => n, StringComparer.Ordinal),
    ];

    public string? GetDatasetJson(string datasetName)
    {
        if (string.IsNullOrWhiteSpace(datasetName)) return null;

        var resource = $"{Prefix}{datasetName}{Suffix}";
        using var stream = Assembly.GetManifestResourceStream(resource);
        if (stream is null) return null;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
