namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Reciprocal rank fusion: each ranking contributes 1 / (k + rank) for every
/// document, so a document near the top of either list rises without the two
/// scoring scales having to be comparable. The knowledge retriever fuses BM25 with
/// embedding similarity this way when a hosted embedding model is configured.
/// </summary>
public static class RankFusion
{
    /// <summary>The conventional damping constant; it limits how much the very top rank dominates.</summary>
    public const int DefaultK = 60;

    /// <summary>
    /// Fused score for each document, given one score array per ranking (same
    /// document order in every array, higher is better). A document with a zero
    /// score in a ranking is treated as absent from it.
    /// </summary>
    public static double[] Fuse(IReadOnlyList<double[]> rankings, int k = DefaultK)
    {
        ArgumentNullException.ThrowIfNull(rankings);
        if (rankings.Count == 0) return [];
        var n = rankings[0].Length;
        var fused = new double[n];
        foreach (var scores in rankings)
        {
            var order = Enumerable.Range(0, n)
                .Where(i => scores[i] > 0)
                .OrderByDescending(i => scores[i])
                .ThenBy(i => i)
                .ToList();
            for (var rank = 0; rank < order.Count; rank++)
            {
                fused[order[rank]] += 1.0 / (k + rank + 1);
            }
        }
        return fused;
    }
}
