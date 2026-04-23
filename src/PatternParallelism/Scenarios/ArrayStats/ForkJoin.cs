namespace PatternParallelism.Scenarios.ArrayStats;

internal static class ForkJoin
{
    internal static async Task<ScanResult> Run(double[] data, int threads)
    {
        var depth = (int)Math.Ceiling(Math.Log2(Math.Max(1, threads)));
        (var min, var max, var sum) = await Core(data, 0, data.Length, depth);
        return new ScanResult(min, max, sum / data.Length, Common.ComputeMedian(data, threads));
    }

    private static async Task<(double Min, double Max, double Sum)> Core(
        double[] data,
        int start,
        int end,
        int depth)
    {
        if (depth == 0)
        {
            return Common.PartialScan(data, start, end);
        }

        var mid = (start + end) / 2;
        var left = Task.Run(() => Core(data, start, mid, depth - 1));
        var right = Task.Run(() => Core(data, mid, end, depth - 1));
        var result = await Task.WhenAll(left, right);
        (var lMin, var lMax, var lSum) = result[0];
        (var rMin, var rMax, var rSum) = result[1];

        return (Math.Min(lMin, rMin), Math.Max(lMax, rMax), lSum + rSum);
    }
}
