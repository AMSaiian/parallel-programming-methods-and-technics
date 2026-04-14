namespace PatternParallelism.Scenarios.MatMul;

internal static class Sequential
{
    internal static double[,] Run(double[,] a,
                                  double[,] b,
                                  int n)
    {
        var c = new double[n, n];
        for (var i = 0; i < n; i++)
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
        return c;
    }
}
