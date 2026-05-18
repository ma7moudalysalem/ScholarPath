namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Turns text into a dense vector for the RAG knowledge base. Two providers
/// implement this: a deterministic offline one (feature hashing — no key, no
/// cost) and an Azure OpenAI one (<c>text-embedding-3-small</c>).
///
/// Vectors from different providers live in different spaces, so every
/// <c>KnowledgeDocument</c> records the <see cref="ModelName"/> that produced
/// its embedding; retrieval only compares vectors from the same model.
/// </summary>
public interface IEmbeddingService
{
    /// <summary>Identifier stamped onto <c>KnowledgeDocument.EmbeddingModel</c>.</summary>
    string ModelName { get; }

    /// <summary>Dimensionality of the vectors this provider produces.</summary>
    int Dimensions { get; }

    /// <summary>Embeds a single text. Returns a zero-length array for empty input.</summary>
    Task<float[]> EmbedAsync(string text, CancellationToken ct);

    /// <summary>
    /// Embeds a batch in input order. Providers that support native batching
    /// (Azure) issue one request; others embed sequentially.
    /// </summary>
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct);
}
