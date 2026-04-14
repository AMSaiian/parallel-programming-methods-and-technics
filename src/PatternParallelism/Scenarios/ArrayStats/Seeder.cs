namespace PatternParallelism.Scenarios.ArrayStats;

internal static class Seeder
{
    internal static double[] Seed(int size,
                                  int seed)
    {
        var rng = new Random(seed);
        var arr = new double[size];
        for (var i = 0; i < size; i++)
        {
            arr[i] = rng.NextDouble() * 1_000_000.0;
        }
        return arr;
    }

    internal static (double Min, double Max, double Sum) PartialScan(double[] data,
                                                                     int start,
                                                                     int end)
    {
        var min = data[start];
        var max = data[start];
        var sum = 0.0;
        for (var i = start; i < end; i++)
        {
            var v = data[i];
            if (v < min)
            {
                min = v;
            }
            if (v > max)
            {
                max = v;
            }
            sum += v;
        }
        return (min, max, sum);
    }

    internal static double ComputeMedian(double[] data,
                                         bool parallel)
    {
        double[] sorted;
        if (parallel)
        {
            sorted = data.AsParallel().OrderBy(x => x).ToArray();
        }
        else
        {
            sorted = (double[])data.Clone();
            Array.Sort(sorted);
        }
        var n = sorted.Length;
        return n % 2 == 0
            ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0
            : sorted[n / 2];
    }
}
