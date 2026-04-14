using System.Threading.Channels;

namespace PatternParallelism.Scenarios.MatMul;

internal static class WorkerPool
{
    internal static async Task<double[,]> Run(double[,] a,
                                              double[,] b,
                                              int n,
                                              int threads)
    {
        var c = new double[n, n];
        var channel = Channel.CreateUnbounded<int>(
            new UnboundedChannelOptions { SingleWriter = true });

        var workers = Enumerable.Range(0, threads).Select(_ => Task.Run(async () =>
        {
            await foreach (var i in channel.Reader.ReadAllAsync())
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
        })).ToArray();

        for (var i = 0; i < n; i++)
        {
            await channel.Writer.WriteAsync(i);
        }
        channel.Writer.Complete();

        await Task.WhenAll(workers);
        return c;
    }
}
