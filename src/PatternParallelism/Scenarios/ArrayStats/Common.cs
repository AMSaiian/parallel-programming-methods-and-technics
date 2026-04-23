namespace PatternParallelism.Scenarios.ArrayStats;

internal static class Common
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
            var element = data[i];
            if (element < min)
            {
                min = element;
            }
            if (element > max)
            {
                max = element;
            }
            sum += element;
        }
        return (min, max, sum);
    }

    internal static double ComputeMedian(double[] data,
                                         int threads)
    {
        if (threads > 1)
        {
            var buf = (double[])data.Clone();

            var chunkSize = (int)Math.Ceiling(buf.Length / (double)threads);
            var chunks = Enumerable.Range(0, threads)
                                   .Select(t => (
                                       Start: t * chunkSize,
                                       End: Math.Min((t + 1) * chunkSize, buf.Length)))
                                   .Where(c => c.Start < c.End)
                                   .ToArray();

            Parallel.ForEach(chunks, c => Array.Sort(buf, c.Start, c.End - c.Start));

            var n = buf.Length;
            return n % 2 == 0
                ? (FindByIndex(buf, chunks, n / 2 - 1) + FindByIndex(buf, chunks, n / 2)) / 2.0
                : FindByIndex(buf, chunks, n / 2);
        }
        else
        {
            var sorted = (double[])data.Clone();
            Array.Sort(sorted);
            var n = sorted.Length;
            return n % 2 == 0
                ? (sorted[n / 2 - 1] + sorted[n / 2]) / 2.0
                : sorted[n / 2];
        }
    }

    private static double FindByIndex(double[] buf, (int Start, int End)[] chunks, int needIndex)
    {
        foreach (var guessChunk in chunks)
        {
            var guessLowIndex = guessChunk.Start;
            var guessHighIndex = guessChunk.End - 1;

            while (guessLowIndex <= guessHighIndex)
            {
                var guessIndex = guessLowIndex + (guessHighIndex - guessLowIndex) / 2;
                var guessedValue = buf[guessIndex];

                var countStrictlyLess = 0;
                var countLessOrEqual = 0;

                foreach (var targetChunk in chunks)
                {
                    var searchLow = targetChunk.Start;
                    var searchHigh = targetChunk.End;

                    while (searchLow < searchHigh)
                    {
                        var searchMid = searchLow + (searchHigh - searchLow) / 2;
                        if (buf[searchMid] < guessedValue)
                        {
                            searchLow = searchMid + 1;
                        }
                        else
                        {
                            searchHigh = searchMid;
                        }
                    }
                    countStrictlyLess += searchLow - targetChunk.Start;

                    searchLow = targetChunk.Start;
                    searchHigh = targetChunk.End;

                    while (searchLow < searchHigh)
                    {
                        var searchMid = searchLow + (searchHigh - searchLow) / 2;
                        if (buf[searchMid] <= guessedValue)
                        {
                            searchLow = searchMid + 1;
                        }
                        else
                        {
                            searchHigh = searchMid;
                        }
                    }
                    countLessOrEqual += searchLow - targetChunk.Start;
                }

                if (needIndex >= countStrictlyLess && needIndex < countLessOrEqual)
                {
                    return guessedValue;
                }

                if (needIndex >= countLessOrEqual)
                {
                    guessLowIndex = guessIndex + 1;
                }
                else
                {
                    guessHighIndex = guessIndex - 1;
                }
            }
        }

        throw new InvalidOperationException($"k-th element not found (k={needIndex})");
    }
}
