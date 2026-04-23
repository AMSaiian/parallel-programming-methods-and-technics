using System.Threading.Channels;

namespace PatternParallelism.Scenarios.Transactions;

// Pipeline pattern — 4 stages connected by bounded channels carrying batches:
//   Reader  →  [Stage0: Parse]  →  [Stage1: Currency conversion]  →  [Stage2: Cashback]  →  [Stage3: Aggregation]
// Each compute stage runs N parallel workers; all stages overlap concurrently.
internal static class Pipeline
{
    private const int BatchSize = 512;

    internal static async Task<AggregationResult> Run(string filePath, int threads)
    {
        var opts = new BoundedChannelOptions(threads * 4) { SingleWriter = false, SingleReader = false };
        var toParser      = Channel.CreateBounded<string[]>(opts);
        var toConversion  = Channel.CreateBounded<Transaction[]>(opts);
        var toCashback    = Channel.CreateBounded<ConvertedTransaction[]>(opts);
        var toAggregation = Channel.CreateBounded<ProcessedTransaction[]>(opts);

        var readerTask = Task.Run(async () =>
        {
            var batch = new List<string>(BatchSize);
            foreach (var line in File.ReadLines(filePath).Skip(1))
            {
                batch.Add(line);
                if (batch.Count == BatchSize)
                {
                    await toParser.Writer.WriteAsync(batch.ToArray());
                    batch.Clear();
                }
            }
            if (batch.Count > 0)
            {
                await toParser.Writer.WriteAsync(batch.ToArray());
            }
            toParser.Writer.Complete();
        });

        // Stage 0: parallel CSV batch parsing
        var stage0Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var lines in toParser.Reader.ReadAllAsync())
                {
                    var parsed = new Transaction[lines.Length];
                    for (var i = 0; i < lines.Length; i++)
                    {
                        parsed[i] = Common.ParseLine(lines[i]);
                    }
                    await toConversion.Writer.WriteAsync(parsed);
                }
            }))
            .ToArray();

        // Stage 1: currency conversion
        var stage1Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var txs in toConversion.Reader.ReadAllAsync())
                {
                    var converted = new ConvertedTransaction[txs.Length];
                    for (var i = 0; i < txs.Length; i++)
                    {
                        converted[i] = new ConvertedTransaction(txs[i].UserId, Common.ConvertToUsd(txs[i].Amount, txs[i].Currency));
                    }
                    await toCashback.Writer.WriteAsync(converted);
                }
            }))
            .ToArray();

        // Stage 2: cashback application
        var stage2Tasks = Enumerable.Range(0, threads)
            .Select(_ => Task.Run(async () =>
            {
                await foreach (var converteds in toCashback.Reader.ReadAllAsync())
                {
                    var processed = new ProcessedTransaction[converteds.Length];
                    for (var i = 0; i < converteds.Length; i++)
                    {
                        var c = converteds[i];
                        processed[i] = new ProcessedTransaction(
                            c.AmountUsd,
                            Common.ApplyCashback(c.AmountUsd, c.UserId),
                            Common.IsPremium(c.UserId));
                    }
                    await toAggregation.Writer.WriteAsync(processed);
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
            await foreach (var processeds in toAggregation.Reader.ReadAllAsync())
            {
                foreach (var p in processeds)
                {
                    totalBefore += p.AmountUsd;
                    totalAfter += p.FinalAmount;
                    totalCount++;
                    if (p.IsPremium)
                    {
                        premiumCount++;
                    }
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
