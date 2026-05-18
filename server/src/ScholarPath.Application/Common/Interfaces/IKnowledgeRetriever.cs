using ScholarPath.Domain.Enums;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// The "retrieval" half of Retrieval-Augmented Generation: embeds a query and
/// returns the most semantically similar knowledge-base documents, ranked by
/// cosine similarity.
/// </summary>
public interface IKnowledgeRetriever
{
    /// <summary>
    /// Retrieves the <paramref name="topK"/> documents most relevant to
    /// <paramref name="query"/>. Returns an empty list when the knowledge base
    /// has not been indexed yet (no embedded documents for the active model).
    /// </summary>
    Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
        string query, int topK, CancellationToken ct);
}

/// <summary>A knowledge-base document returned by a retrieval, with its relevance score.</summary>
public sealed record RetrievedDocument(
    Guid DocumentId,
    KnowledgeSourceType SourceType,
    Guid? SourceId,
    string TitleEn,
    string TitleAr,
    string ContentEn,
    string ContentAr,
    string? MetadataJson,
    /// <summary>Cosine similarity in [-1, 1]; higher is more relevant.</summary>
    double Score);
