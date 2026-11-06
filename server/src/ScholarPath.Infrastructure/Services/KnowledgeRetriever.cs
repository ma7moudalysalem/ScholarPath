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

        // Without a trained embedding model the hashing vectors carry little
        // meaning, so the fallback ranks by BM25 instead. English and Arabic
        // are indexed separately so a question in either language is scored
        // against text in its own language.
        if (model == LocalEmbeddingService.Model)
            return await RetrieveLexicalAsync(query, topK, ct).ConfigureAwait(false);

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

        // Embedding similarity and BM25 are fused by reciprocal rank: a document
        // near the top of either ranking rises, so an exact term the embedding
        // blurs (a country, a field) still counts. Measured above either alone.
        var cosine = docs.Select(d => Math.Max(0d, VectorMath.CosineSimilarity(queryVector, VectorMath.Unpack(d.Embedding)))).ToArray();
        var lexical = LexicalScores([.. docs.Select(d => (d.TitleEn + " " + d.ContentEn, d.TitleAr + " " + d.ContentAr))], query);
        var fused = RankFusion.Fuse([cosine, lexical]);

        // Ordered by the fused rank; the score reported with each document stays
        // its embedding similarity, which the relevance floor (RagMinScore) reads.
        return [.. docs
            .Select((d, i) => new
            {
                Doc = d,
                Fused = fused[i],
                Score = cosine[i],
            })
            .OrderByDescending(x => x.Fused)
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

    private async Task<IReadOnlyList<RetrievedDocument>> RetrieveLexicalAsync(
        string query, int topK, CancellationToken ct)
    {
        var docs = await db.KnowledgeDocuments
            .AsNoTracking()
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
            })
            .ToListAsync(ct)
            .ConfigureAwait(false);
        if (docs.Count == 0) return [];

        var scores = LexicalScores([.. docs.Select(d => (d.TitleEn + " " + d.ContentEn, d.TitleAr + " " + d.ContentAr))], query);

        return [.. docs
            .Select((d, i) => new
            {
                Doc = d,
                Score = scores[i],
            })
            .Where(x => x.Score > 0)
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

    /// <summary>
    /// BM25 over the English and the Arabic text of each document, each scaled
    /// to its best match, so a question in either language is scored against text
    /// in its own language.
    /// </summary>
    public static double[] LexicalScores(IReadOnlyList<(string En, string Ar)> docs, string query)
    {
        var en = new Bm25Index([.. docs.Select(d => d.En)]);
        var ar = new Bm25Index([.. docs.Select(d => d.Ar)]);
        var enScores = en.Score(query);
        var arScores = ar.Score(query);
        var enMax = en.MaxScore(query);
        var arMax = ar.MaxScore(query);
        return [.. Enumerable.Range(0, docs.Count).Select(i => Math.Max(
            enMax > 0 ? enScores[i] / enMax : 0,
            arMax > 0 ? arScores[i] / arMax : 0))];
    }
}
