using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Persistence;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// In-process RAG retriever. Embeds the query, then ranks the knowledge base by
/// cosine similarity and returns the top matches.
///
/// Only documents embedded with the <em>active</em> model are considered —
/// vectors from a different embedding model live in a different space, so they
/// are skipped until the knowledge base is re-indexed. The corpus is small
/// (a few hundred rows) so a full in-memory scan per query is more than fast
/// enough and avoids standing up a dedicated vector database.
/// </summary>
public sealed class KnowledgeRetriever(
    ApplicationDbContext db,
    IEmbeddingService embeddings) : IKnowledgeRetriever
{
    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(
        string query, int topK, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query) || topK <= 0)
            return [];

        var model = embeddings.ModelName;

        var docs = await db.KnowledgeDocuments
            .AsNoTracking()
            .Where(d => d.EmbeddingModel == model && d.EmbeddingDimensions > 0)
            .Select(d => new
            {
                d.Id,
                d.SourceType,
                d.SourceId,
                d.TitleEn,
                d.TitleAr,
                d.ContentEn,
                d.ContentAr,
                d.MetadataJson,
                d.Embedding,
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);

        if (docs.Count == 0) return [];

        var queryVector = await embeddings.EmbedAsync(query, ct).ConfigureAwait(false);
        if (queryVector.Length == 0) return [];

        return [.. docs
            .Select(d => new
            {
                Doc = d,
                Score = VectorMath.CosineSimilarity(queryVector, VectorMath.Unpack(d.Embedding)),
            })
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .Select(x => new RetrievedDocument(
                x.Doc.Id,
                x.Doc.SourceType,
                x.Doc.SourceId,
                x.Doc.TitleEn,
                x.Doc.TitleAr,
                x.Doc.ContentEn,
                x.Doc.ContentAr,
                x.Doc.MetadataJson,
                x.Score))];
    }
}
