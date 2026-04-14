using System.Threading.Channels;

namespace PatternParallelism.Scenarios.ArrayStats;

internal static class WorkerPull
{
    internal static async Task<ScanResult> Run(double[] data, int threads)
    {
        var chunkSize = (int)Math.Ceiling(data.Length / (double)threads);
        var channel = Channel.CreateUnbounded<(int Start, int End)>(
            new UnboundedChannelOptions { SingleWriter = true });

        var results = new (double Min, double Max, double Sum)[threads];

        var workers = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
        {
            var lMin = double.MaxValue;
            var lMax = double.MinValue;
            var lSum = 0.0;
            await foreach ((var s, var e) in channel.Reader.ReadAllAsync())
            {
                (var pMin, var pMax, var pSum) = Seeder.PartialScan(data, s, e);
                if (pMin < lMin)
                {
                    lMin = pMin;
                }
                if (pMax > lMax)
                {
                    lMax = pMax;
                }
                lSum += pSum;
            }
            results[t] = (lMin, lMax, lSum);
        })).ToArray();

        for (var i = 0; i < data.Length; i += chunkSize)
        {
            await channel.Writer.WriteAsync((i, Math.Min(i + chunkSize, data.Length)));
        }
        channel.Writer.Complete();

        await Task.WhenAll(workers);

        var min = double.MaxValue;
        var max = double.MinValue;
        var sumAll = 0.0;
        foreach (var r in results)
        {
            (var rMin, var rMax, var rSum) = r;
            if (rMin < min)
            {
                min = rMin;
            }
            if (rMax > max)
            {
                max = rMax;
            }
            sumAll += rSum;
        }
        return new ScanResult(min, max, sumAll / data.Length, Seeder.ComputeMedian(data, parallel: true));
    }
}
