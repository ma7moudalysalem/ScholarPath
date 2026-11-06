using System.Text.RegularExpressions;

namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Okapi BM25 over a fixed set of texts. It is the retriever used when no trained
/// embedding model is configured: on the platform's own known-item benchmark a
/// lexical index finds the right listing far more often than the hashing
/// embedder it replaces, and it needs no key and no network.
///
/// Scores are divided by the best score the query could reach (every term
/// present with saturating frequency), so they fall in [0, 1] and the same
/// relevance threshold applies as for cosine similarity.
/// </summary>
public sealed partial class Bm25Index
{
    private const double K1 = 1.2;
    private const double B = 0.75;

    private readonly List<Dictionary<string, int>> _tf;
    private readonly int[] _lengths;
    private readonly Dictionary<string, int> _df = new(StringComparer.Ordinal);
    private readonly double _avgLength;

    public Bm25Index(IReadOnlyList<string> texts)
    {
        _tf = new List<Dictionary<string, int>>(texts.Count);
        _lengths = new int[texts.Count];
        for (var i = 0; i < texts.Count; i++)
        {
            var tokens = Tokenize(texts[i]);
            _lengths[i] = tokens.Count;
            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var t in tokens) counts[t] = counts.GetValueOrDefault(t) + 1;
            _tf.Add(counts);
            foreach (var t in counts.Keys) _df[t] = _df.GetValueOrDefault(t) + 1;
        }
        _avgLength = texts.Count == 0 ? 0 : _lengths.Average();
    }

    public int Count => _tf.Count;

    /// <summary>Case-folded letter/digit runs of two or more characters, in any script.</summary>
    public static List<string> Tokenize(string text) =>
        [.. TokenSplit().Split(text.ToUpperInvariant()).Where(t => t.Length > 1)];

    /// <summary>Raw BM25 score of every text for <paramref name="query"/>, in input order.</summary>
    public double[] Score(string query)
    {
        var scores = new double[_tf.Count];
        if (_tf.Count == 0 || _avgLength <= 0) return scores;
        foreach (var t in Tokenize(query).Distinct())
        {
            if (!_df.TryGetValue(t, out var df)) continue;
            var idf = Idf(df);
            for (var i = 0; i < _tf.Count; i++)
            {
                if (!_tf[i].TryGetValue(t, out var f)) continue;
                scores[i] += idf * f * (K1 + 1) / (f + K1 * (1 - B + B * _lengths[i] / _avgLength));
            }
        }
        return scores;
    }

    /// <summary>The score a text would get if it contained every query term often.</summary>
    public double MaxScore(string query) =>
        Tokenize(query).Distinct().Sum(t => _df.TryGetValue(t, out var df) ? Idf(df) * (K1 + 1) : 0);

    private double Idf(int df) => Math.Log(1 + (_tf.Count - df + 0.5) / (df + 0.5));

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex TokenSplit();
}
