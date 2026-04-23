using System.Threading.Channels;

namespace PatternParallelism.Scenarios.Transactions;

// Producer-Consumer pattern: the producer streams raw CSV lines into a bounded channel;
// N consumer workers each parse lines and apply the full processing pipeline
// (conversion + cashback), accumulating private partial sums to avoid contention.
// The final aggregation merges partial sums after all consumers finish.
internal static class ProducerConsumer
{
    internal static async Task<AggregationResult> Run(string filePath, int threads)
    {
        var channel = Channel.CreateBounded<string>(
            new BoundedChannelOptions(threads * 8) { SingleWriter = true, SingleReader = false });

        // Producer: reads raw lines (skips header), feeds consumer pool
        var producerTask = Task.Run(async () =>
        {
            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                await channel.Writer.WriteAsync(line);
            }
            channel.Writer.Complete();
        });

        var partialBefore = new decimal[threads];
        var partialAfter = new decimal[threads];
        var partialPremiumCounts = new long[threads];
        var partialTotalCounts = new long[threads];

        var consumerTasks = Enumerable.Range(0, threads)
            .Select(i => Task.Run(async () =>
            {
                decimal localBefore = 0m;
                decimal localAfter = 0m;
                long localPremium = 0;
                long localCount = 0;

                await foreach (var line in channel.Reader.ReadAllAsync())
                {
                    var tx = Common.ParseLine(line);
                    var amountUsd = Common.ConvertToUsd(tx.Amount, tx.Currency);
                    localBefore += amountUsd;
                    localAfter += Common.ApplyCashback(amountUsd, tx.UserId);
                    localCount++;
                    if (Common.IsPremium(tx.UserId))
                    {
                        localPremium++;
                    }
                }

                partialBefore[i] = localBefore;
                partialAfter[i] = localAfter;
                partialPremiumCounts[i] = localPremium;
                partialTotalCounts[i] = localCount;
            }))
            .ToArray();

        await producerTask;
        await Task.WhenAll(consumerTasks);

        return new AggregationResult(
            TotalBeforeCashback: partialBefore.Aggregate((a, b) => a + b),
            TotalAmountUsd: partialAfter.Aggregate((a, b) => a + b),
            PremiumCount: partialPremiumCounts.Sum(),
            TotalCount: partialTotalCounts.Sum()
        );
    }
}
