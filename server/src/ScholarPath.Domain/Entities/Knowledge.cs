using ScholarPath.Domain.Common;
using ScholarPath.Domain.Enums;

namespace ScholarPath.Domain.Entities;

/// <summary>
/// A single retrievable document in the Retrieval-Augmented-Generation (RAG)
/// knowledge base — one row per indexed scholarship or curated FAQ entry.
///
/// The embedding vector is stored as a packed little-endian <c>float[]</c> in
/// <see cref="Embedding"/>; retrieval loads the candidate rows and ranks them
/// by cosine similarity in-process. The corpus is a few hundred rows at most,
/// so a dedicated vector database would be overkill — and an in-process scan
/// keeps the design free of an extra Azure resource.
/// </summary>
public class KnowledgeDocument : AuditableEntity
{
    public KnowledgeSourceType SourceType { get; set; }

    /// <summary>Scholarship id when <see cref="SourceType"/> is Scholarship; null for FAQ docs.</summary>
    public Guid? SourceId { get; set; }

    /// <summary>Stable natural key used for idempotent upsert (scholarship id, or FAQ key).</summary>
    public string SourceKey { get; set; } = default!;

    public string TitleEn { get; set; } = default!;
    public string TitleAr { get; set; } = default!;

    /// <summary>The retrievable body text — what the embedding is computed from and what is injected as grounding context.</summary>
    public string ContentEn { get; set; } = default!;
    public string ContentAr { get; set; } = default!;

    /// <summary>SHA-256 of the content; lets a re-index skip documents whose text is unchanged.</summary>
    public string ContentHash { get; set; } = default!;

    /// <summary>Packed little-endian <c>float[]</c> embedding. Empty until the document is embedded.</summary>
    public byte[] Embedding { get; set; } = [];

    public int EmbeddingDimensions { get; set; }

    /// <summary>The model that produced <see cref="Embedding"/> (e.g. "text-embedding-3-small", "local-hash-v1").</summary>
    public string? EmbeddingModel { get; set; }

    public DateTimeOffset? IndexedAt { get; set; }

    /// <summary>Optional JSON metadata (deadline, funding, country, URL...) surfaced as citation detail.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>True once <see cref="Embedding"/> holds a current vector for <see cref="ContentHash"/>.</summary>
    public bool IsEmbedded => Embedding.Length > 0 && EmbeddingDimensions > 0;
}
