namespace PatternParallelism.Scenarios.ArrayStats;

internal static class MapReduce
{
    internal static async Task<ScanResult> Run(double[] data,
                                               int threads)
    {
        var chunkSize = (int)Math.Ceiling(data.Length / (double)threads);

        var mapTasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var start = t * chunkSize;
            var end = Math.Min(start + chunkSize, data.Length);
            if (start >= data.Length)
            {
                return (double.MaxValue, double.MinValue, 0.0);
            }
            return Common.PartialScan(data, start, end);
        }));

        var partials = await Task.WhenAll(mapTasks);

        var min = double.MaxValue;
        var max = double.MinValue;
        var totalSum = 0.0;
        foreach (var p in partials)
        {
            (var pMin, var pMax, var pSum) = p;
            if (pMin < min)
            {
                min = pMin;
            }
            if (pMax > max)
            {
                max = pMax;
            }
            totalSum += pSum;
        }

        return new ScanResult(min, max, totalSum / data.Length, Common.ComputeMedian(data, threads));
    }
}
