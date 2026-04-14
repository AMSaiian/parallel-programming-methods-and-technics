namespace PatternParallelism.Scenarios.MatMul;

internal static class ForkJoin
{
    internal static async Task<double[,]> Run(double[,] a,
                                              double[,] b,
                                              int n,
                                              int threads)
    {
        var c = new double[n, n];
        var depth = (int)Math.Ceiling(Math.Log2(Math.Max(1, threads)));
        await Core(a, b, c, n, 0, n, depth);
        return c;
    }

    private static async Task Core(
        double[,] a,
        double[,] b,
        double[,] c,
        int n,
        int rowStart,
        int rowEnd,
        int depth)
    {
        if (depth == 0 || rowEnd - rowStart <= 1)
        {
            for (var i = rowStart; i < rowEnd; i++)
            {
                for (var j = 0; j < n; j++)
                {
                    var sum = 0.0;
                    for (var k = 0; k < n; k++)
                    {
                        sum += a[i, k] * b[k, j];
                    }
                    c[i, j] = sum;
                }
            }
            return;
        }

        var mid = (rowStart + rowEnd) / 2;
        var left = Task.Run(() => Core(a, b, c, n, rowStart, mid, depth - 1));
        var right = Task.Run(() => Core(a, b, c, n, mid, rowEnd, depth - 1));
        await Task.WhenAll(left, right);
    }
}
