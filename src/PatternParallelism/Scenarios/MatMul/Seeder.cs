namespace PatternParallelism.Scenarios.MatMul;

internal static class Seeder
{
    internal static (double[,] A, double[,] B) Seed(int n,
                                                    int seed)
    {
        var rng = new Random(seed);
        var a = new double[n, n];
        var b = new double[n, n];
        for (var i = 0; i < n; i++)
        {
            for (var j = 0; j < n; j++)
            {
                a[i, j] = rng.NextDouble();
                b[i, j] = rng.NextDouble();
            }
        }
        return (a, b);
    }
}
