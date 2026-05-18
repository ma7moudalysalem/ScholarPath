using ScholarPath.Application.Ai.DTOs;

namespace ScholarPath.Application.Common.Interfaces;

/// <summary>
/// Builds and maintains the RAG knowledge base — it turns scholarships and the
/// curated FAQ dataset into embedded <c>KnowledgeDocument</c> rows that the
/// retriever searches at chat time.
/// </summary>
public interface IKnowledgeBaseIndexer
{
    /// <summary>
    /// Rebuilds the knowledge base: upserts a document per open scholarship and
    /// per FAQ entry, removes stale rows, and (re)embeds everything that is new,
    /// changed, or was embedded with a different model. Idempotent — safe to run
    /// repeatedly. <paramref name="force"/> re-embeds every document regardless.
    /// </summary>
    Task<KnowledgeBaseRebuildResultDto> RebuildAsync(bool force, CancellationToken ct);

    /// <summary>Current index status — document counts, active model, last index time.</summary>
    Task<KnowledgeBaseStatusDto> GetStatusAsync(CancellationToken ct);
}
