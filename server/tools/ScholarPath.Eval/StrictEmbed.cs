using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Eval;

/// <summary>
/// The hosted embedding service answers a failed call with offline vectors so the
/// platform keeps working. A measurement cannot accept that: vectors from two
/// models live in different spaces. Every batch is therefore checked for the
/// provider's own dimensionality, retried after a pause, and the run stops if the
/// hosted model still does not answer.
/// </summary>
public static class StrictEmbed
{
    public static async Task<IReadOnlyList<float[]>> BatchAsync(IEmbeddingService svc, IReadOnlyList<string> texts)
    {
        for (var attempt = 1; ; attempt++)
        {
            var vectors = await svc.EmbedBatchAsync(texts, CancellationToken.None).ConfigureAwait(false);
            if (vectors.All(v => v.Length == svc.Dimensions)) return vectors;
            if (attempt == 8)
                throw new InvalidOperationException($"{svc.ModelName}: the hosted model kept failing; the run is stopped rather than mixing vector spaces.");
            await Task.Delay(TimeSpan.FromSeconds(5 * attempt)).ConfigureAwait(false);
        }
    }
}
