using System.Threading.Channels;

namespace PatternParallelism.Scenarios.Transactions;

// Producer-Consumer pattern: the producer streams raw CSV lines into a bounded channel
// as batches; N consumer workers each process a full batch (parse + convert + cashback),
// accumulating private partial sums to avoid contention.
// The final aggregation merges partial sums after all consumers finish.
internal static class ProducerConsumer
{
    private const int BatchSize = 1024;

    internal static async Task<AggregationResult> Run(string filePath, int threads)
    {
        var channel = Channel.CreateBounded<string[]>(
            new BoundedChannelOptions(threads * 4) { SingleWriter = true, SingleReader = false });

        var producerTask = Task.Run(async () =>
        {
            var batch = new List<string>(BatchSize);
            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                batch.Add(line);
                if (batch.Count == BatchSize)
                {
                    await channel.Writer.WriteAsync(batch.ToArray());
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                await channel.Writer.WriteAsync(batch.ToArray());
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

                await foreach (var lines in channel.Reader.ReadAllAsync())
                {
                    foreach (var line in lines)
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
