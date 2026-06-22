namespace Rag.Shared.Extensions;
public static class FloatExtensions
{
    public static float[] Normalize(this float[] vector)
    {
        float magnitude = MathF.Sqrt(vector.Sum(v => v * v));
        if (magnitude == 0) return vector;
        return vector.Select(v => v / magnitude).ToArray();
    }

    public static double CosineSimilarity(this float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        if (magA == 0 || magB == 0) return 0;
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}
