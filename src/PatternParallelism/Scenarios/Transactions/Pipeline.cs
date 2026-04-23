using System.Threading.Channels;

namespace PatternParallelism.Scenarios.Transactions;

// Pipeline pattern — 4 stages connected by bounded channels:
//   Reader  →  [Stage0: Parse]  →  [Stage1: Currency conversion]  →  [Stage2: Cashback]  →  [Stage3: Aggregation]
// Each compute stage runs N parallel workers; all stages overlap concurrently.
internal static class Pipeline
{
    internal static async Task<AggregationResult> Run(string filePath, int threads)
    {
        var opts = new BoundedChannelOptions(threads * 8) { SingleWriter = false, SingleReader = false };
        var toParser      = Channel.CreateBounded<string>(opts);
        var toConversion  = Channel.CreateBounded<Transaction>(opts);
        var toCashback    = Channel.CreateBounded<ConvertedTransaction>(opts);
        var toAggregation = Channel.CreateBounded<ProcessedTransaction>(opts);

        // Reader: streams raw CSV lines (skipping header) into Stage 0
        var readerTask = Task.Run(async () =>
        {
            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                await toParser.Writer.WriteAsync(line);
            }
            toParser.Writer.Complete();
        });

        // Stage 0: parallel CSV line parsing
        var stage0Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var line in toParser.Reader.ReadAllAsync())
                {
                    await toConversion.Writer.WriteAsync(Common.ParseLine(line));
                }
            }))
            .ToArray();

        // Stage 1: currency conversion
        var stage1Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var tx in toConversion.Reader.ReadAllAsync())
                {
                    var amountUsd = Common.ConvertToUsd(tx.Amount, tx.Currency);
                    await toCashback.Writer.WriteAsync(new ConvertedTransaction(tx.UserId, amountUsd));
                }
            }))
            .ToArray();

        // Stage 2: cashback application
        var stage2Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var converted in toCashback.Reader.ReadAllAsync())
                {
                    var isPremium = Common.IsPremium(converted.UserId);
                    var finalAmount = Common.ApplyCashback(converted.AmountUsd, converted.UserId);
                    await toAggregation.Writer.WriteAsync(
                        new ProcessedTransaction(converted.AmountUsd, finalAmount, isPremium));
                }
            }))
            .ToArray();

        // Stage 3: aggregation (single consumer — no contention needed)
        var stage3Task = Task.Run(async () =>
        {
            decimal totalBefore = 0m;
            decimal totalAfter = 0m;
            long premiumCount = 0;
            long totalCount = 0;
            await foreach (var processed in toAggregation.Reader.ReadAllAsync())
            {
                totalBefore += processed.AmountUsd;
                totalAfter += processed.FinalAmount;
                totalCount++;
                if (processed.IsPremium)
                {
                    premiumCount++;
                }
            }
            return new AggregationResult(totalBefore, totalAfter, premiumCount, totalCount);
        });

        // Coordinate completion: each stage signals the next when all its workers finish
        await readerTask;
        await Task.WhenAll(stage0Tasks);
        toConversion.Writer.Complete();
        await Task.WhenAll(stage1Tasks);
        toCashback.Writer.Complete();
        await Task.WhenAll(stage2Tasks);
        toAggregation.Writer.Complete();
        return await stage3Task;
    }
}
