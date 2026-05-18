namespace ScholarPath.Infrastructure.Services;

/// <summary>
/// Vector helpers for the RAG knowledge base: packs <c>float[]</c> embeddings
/// into the <c>byte[]</c> storage column and back, and scores cosine
/// similarity for retrieval ranking.
/// </summary>
internal static class VectorMath
{
    /// <summary>Packs a <c>float[]</c> into little-endian bytes for storage.</summary>
    public static byte[] Pack(float[] vector)
    {
        if (vector.Length == 0) return [];
        var bytes = new byte[vector.Length * sizeof(float)];
        Buffer.BlockCopy(vector, 0, bytes, 0, bytes.Length);
        return bytes;
    }

    /// <summary>Unpacks little-endian bytes back into a <c>float[]</c>.</summary>
    public static float[] Unpack(byte[] bytes)
    {
        if (bytes.Length == 0) return [];
        var vector = new float[bytes.Length / sizeof(float)];
        Buffer.BlockCopy(bytes, 0, vector, 0, vector.Length * sizeof(float));
        return vector;
    }

    /// <summary>L2-normalizes a vector in place and returns it.</summary>
    public static float[] Normalize(float[] vector)
    {
        double sumSq = 0;
        for (var i = 0; i < vector.Length; i++) sumSq += (double)vector[i] * vector[i];

        var norm = Math.Sqrt(sumSq);
        if (norm < 1e-9) return vector;

        for (var i = 0; i < vector.Length; i++) vector[i] = (float)(vector[i] / norm);
        return vector;
    }

    /// <summary>
    /// Cosine similarity of two vectors. When both are L2-normalized this is a
    /// plain dot product; returns 0 on a length mismatch or empty input.
    /// </summary>
    public static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length == 0 || a.Length != b.Length) return 0;

        double dot = 0, normA = 0, normB = 0;
        for (var i = 0; i < a.Length; i++)
        {
            dot += (double)a[i] * b[i];
            normA += (double)a[i] * a[i];
            normB += (double)b[i] * b[i];
        }

        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom < 1e-9 ? 0 : dot / denom;
    }
}
