namespace PatternParallelism.Scenarios.MatMul;

internal static class MapReduce
{
    internal static async Task<double[,]> Run(double[,] a,
                                              double[,] b,
                                              int n,
                                              int threads)
    {
        var c = new double[n, n];
        var chunkSize = (int)Math.Ceiling(n / (double)threads);

        var mapTasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var rowStart = t * chunkSize;
            var rowEnd = Math.Min(rowStart + chunkSize, n);
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
        }));

        await Task.WhenAll(mapTasks);
        return c;
    }
}
