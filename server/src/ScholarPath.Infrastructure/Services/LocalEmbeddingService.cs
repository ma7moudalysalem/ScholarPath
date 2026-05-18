using System.Text;
using ScholarPath.Application.Common.Interfaces;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Deterministic, offline embedding provider — no API key, no network, no cost.
///
/// It uses the "hashing trick": every word unigram and bigram is hashed into a
/// fixed-width vector bucket (with a signed-hash so collisions tend to cancel),
/// then the vector is L2-normalized. Documents that share vocabulary land close
/// together under cosine similarity, so RAG retrieval genuinely works as a
/// lexical-semantic match even with no provider configured.
///
/// Quality is below a transformer embedding model, but it is fully reproducible
/// and lets the entire RAG pipeline run, be tested, and be demoed without an
/// Azure OpenAI key. It is the default provider until Azure credentials are set.
/// </summary>
public sealed class LocalEmbeddingService : IEmbeddingService
{
    private const int Dim = 384;

    public string ModelName => "local-hash-v1";
    public int Dimensions => Dim;

    public Task<float[]> EmbedAsync(string text, CancellationToken ct)
        => Task.FromResult(Embed(text));

    public Task<IReadOnlyList<float[]>> EmbedBatchAsync(
        IReadOnlyList<string> texts, CancellationToken ct)
        => Task.FromResult<IReadOnlyList<float[]>>([.. texts.Select(Embed)]);

    private static float[] Embed(string text)
    {
        var vector = new float[Dim];
        if (string.IsNullOrWhiteSpace(text)) return vector;

        var tokens = Tokenize(text);

        // Unigrams carry the bulk of the signal.
        foreach (var token in tokens)
            Accumulate(vector, token, weight: 1f);

        // Bigrams add a little word-order / phrase signal.
        for (var i = 0; i + 1 < tokens.Count; i++)
            Accumulate(vector, tokens[i] + ' ' + tokens[i + 1], weight: 0.5f);

        return VectorMath.Normalize(vector);
    }

    private static void Accumulate(float[] vector, string feature, float weight)
    {
        var hash = StableHash(feature);
        var bucket = (int)(hash % Dim);
        // A high bit flips the sign so colliding features cancel rather than
        // always reinforce — the standard signed feature-hashing trick.
        var sign = (hash & 0x10000u) == 0 ? 1f : -1f;
        vector[bucket] += weight * sign;
    }

    private static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var sb = new StringBuilder();

        // Case-fold so "Germany" and "germany" hash to the same token. The fold
        // direction is irrelevant for hashing — upper-invariant satisfies CA1308.
        foreach (var ch in text.ToUpperInvariant())
        {
            // char.IsLetterOrDigit is true for Arabic letters/digits too, so
            // this tokenizes English and Arabic alike on whitespace/punctuation.
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                if (sb.Length > 1) tokens.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length > 1) tokens.Add(sb.ToString());

        return tokens;
    }

    /// <summary>FNV-1a — a stable hash independent of the framework's randomized string hashing.</summary>
    private static uint StableHash(string s)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;

        var hash = offsetBasis;
        foreach (var ch in s)
        {
            hash ^= ch;
            hash *= prime;
        }
        return hash;
    }
}
