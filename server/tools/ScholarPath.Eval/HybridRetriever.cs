using Microsoft.EntityFrameworkCore;
using ScholarPath.Application.Common.Interfaces;
using ScholarPath.Infrastructure.Persistence;
using ScholarPath.Infrastructure.Services;

namespace ScholarPath.Eval;

/// <summary>
/// The retriever the platform runs with a hosted embedding model, rebuilt in
/// memory for the harness: the evaluation database stores only the offline
/// vectors, so the knowledge base is embedded once here and ranked as the
/// platform ranks it (cosine similarity). Nothing is
/// written to the database.
/// </summary>
public sealed class HybridRetriever : IKnowledgeRetriever
{
    private readonly IEmbeddingService _embeddings;
    private readonly List<Doc> _docs;
    private readonly List<float[]> _vectors;

    private sealed record Doc(Guid Id, ScholarPath.Domain.Enums.KnowledgeSourceType SourceType, Guid? SourceId,
        string TitleEn, string TitleAr, string ContentEn, string ContentAr, string? MetadataJson);

    private HybridRetriever(IEmbeddingService embeddings, List<Doc> docs, List<float[]> vectors)
    {
        _embeddings = embeddings;
        _docs = docs;
        _vectors = vectors;
    }

    public static async Task<HybridRetriever> BuildAsync(ApplicationDbContext db, IEmbeddingService embeddings)
    {
        var docs = await db.KnowledgeDocuments.AsNoTracking()
            .Select(d => new Doc(d.Id, d.SourceType, d.SourceId, d.TitleEn, d.TitleAr, d.ContentEn, d.ContentAr, d.MetadataJson))
            .ToListAsync().ConfigureAwait(false);
        var vectors = new List<float[]>(docs.Count);
        for (var i = 0; i < docs.Count; i += 64)
        {
            var slice = docs.Skip(i).Take(64).Select(d => d.TitleEn + " " + d.ContentEn).ToList();
            vectors.AddRange(await StrictEmbed.BatchAsync(embeddings, slice).ConfigureAwait(false));
        }
        return new HybridRetriever(embeddings, docs, vectors);
    }

    public async Task<IReadOnlyList<RetrievedDocument>> RetrieveAsync(string query, int topK, CancellationToken ct)
    {
        var qv = (await StrictEmbed.BatchAsync(_embeddings, [query]).ConfigureAwait(false))[0];
        var cosine = _vectors.Select(v => Cosine(qv, v)).ToArray();
        // The platform ranks by cosine similarity alone with a hosted model; the
        // fused ranking was measured in retrieval mode and not adopted.
        return [.. Enumerable.Range(0, _docs.Count)
            .OrderByDescending(i => cosine[i])
            .Take(topK)
            .Select(i => new RetrievedDocument(_docs[i].Id, _docs[i].SourceType, _docs[i].SourceId,
                _docs[i].TitleEn, _docs[i].TitleAr, _docs[i].ContentEn, _docs[i].ContentAr,
                _docs[i].MetadataJson, cosine[i]))];
    }

    private static double Cosine(float[] a, float[] b)
    {
        double dot = 0, na = 0, nb = 0;
        var n = Math.Min(a.Length, b.Length);
        for (var i = 0; i < n; i++) { dot += a[i] * b[i]; na += a[i] * a[i]; nb += b[i] * b[i]; }
        return na <= 0 || nb <= 0 ? 0 : dot / (Math.Sqrt(na) * Math.Sqrt(nb));
    }
}
