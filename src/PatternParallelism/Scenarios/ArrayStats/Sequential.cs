namespace PatternParallelism.Scenarios.ArrayStats;

internal static class Sequential
{
    internal static ScanResult Run(double[] data)
    {
        (var min, var max, var sum) = Seeder.PartialScan(data, 0, data.Length);
        return new ScanResult(min, max, sum / data.Length, Seeder.ComputeMedian(data, parallel: false));
    }
}
