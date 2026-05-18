namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Supplies the raw JSON of the curated datasets bundled with the app — the
/// external scholarships dataset (imported as listings) and the FAQ dataset
/// (indexed into the RAG knowledge base).
/// </summary>
public interface IDatasetProvider
{
    /// <summary>Names of every bundled dataset (without the <c>.json</c> extension).</summary>
    IReadOnlyList<string> AvailableDatasets { get; }

    /// <summary>The raw JSON of a bundled dataset, or <c>null</c> when it is not present.</summary>
    string? GetDatasetJson(string datasetName);
}
