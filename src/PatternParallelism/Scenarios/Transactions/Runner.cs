using System.CommandLine;
using Core;
using Core.Scenarios;

namespace PatternParallelism.Scenarios.Transactions;

public class Runner : BaseScenario
{
    private static readonly Option<string> AlgorithmOption = new("--algo", "-al")
    {
        Description = "Pattern: sequential | pipeline | producerconsumer",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<string>()?.ToLowerInvariant();
                if (value is not ("sequential" or "pipeline" or "producerconsumer"))
                {
                    optionResult.AddError("Algorithm must be one of: sequential, pipeline, producerconsumer.");
                }
            }
        }
    };

    private static readonly Option<int> CountOption = new("--count", "-ct")
    {
        Description = "Number of transactions to generate and write to <count>_Transactions_<seed>.csv (>= 10 000)",
        DefaultValueFactory = _ => 100_000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 10_000)
                {
                    optionResult.AddError("Count must be at least 10 000.");
                }
            }
        }
    };

    public Runner() : base("transactions", "Process financial transactions: currency conversion, cashback, aggregation")
    {
        Options.Add(AlgorithmOption);
        Options.Add(CountOption);
        Options.Add(PatternOptions.WithSequential);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var algo = parseResult.GetValue(AlgorithmOption)!.ToLowerInvariant();
        var count = parseResult.GetValue(CountOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);
        var withSequential = parseResult.GetValue(PatternOptions.WithSequential);

        var filePath = Common.GetFilePath(count, seed);
        if (File.Exists(filePath))
        {
            Console.WriteLine($"  file {filePath} already exists, skipping generation.");
        }
        else
        {
            Console.WriteLine($"  generating {count:N0} transactions → {filePath}...");
            Common.SeedToFile(count, seed);
        }

        Console.WriteLine($"  reading from {filePath}...");

        (long ms, AggregationResult result) = algo switch
        {
            "sequential"       => await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(filePath))),
            "pipeline"         => await ExecuteWithTimingAsync(() => Pipeline.Run(filePath, threads)),
            "producerconsumer" => await ExecuteWithTimingAsync(() => ProducerConsumer.Run(filePath, threads)),
            _                  => throw new InvalidOperationException($"Unknown algorithm: {algo}")
        };

        Console.WriteLine($"  {algo}: took {ms} ms");
        PrintResult(result);

        if (verbose)
        {
            Console.WriteLine("  sample (first 5 transactions from file):");
            foreach (var tx in Common.ReadTransactions(filePath).Take(5))
            {
                var usd = Common.ConvertToUsd(tx.Amount, tx.Currency);
                var final = Common.ApplyCashback(usd, tx.UserId);
                Console.WriteLine($"    user={tx.UserId,5}  {tx.Amount,8:F2} {tx.Currency,-3}"
                                + $"  [{tx.Date}]  [{tx.ProductType,-11}]"
                                + $"  → {usd,8:F2} USD  → {final,8:F2} USD"
                                + $"  premium={Common.IsPremium(tx.UserId)}");
            }
        }

        if (algo != "sequential" && withSequential)
        {
            (var seqMs, var seqResult) = await ExecuteWithTimingAsync(
                () => Task.FromResult(Sequential.Run(filePath)));

            Console.WriteLine($"  sequential: took {seqMs} ms");
            PrintResult(seqResult);

            var speedup = (double)seqMs / ms;
            var efficiency = speedup / threads;
            Console.WriteLine($"  speedup={speedup:F2}x  efficiency={efficiency:F4}");
        }
    }

    private static void PrintResult(AggregationResult r)
    {
        Console.WriteLine($"    before cashback={r.TotalBeforeCashback:F2} USD"
                        + $"  cashback={r.TotalCashback:F2} USD"
                        + $"  total={r.TotalAmountUsd:F2} USD");
        Console.WriteLine($"    premium={r.PremiumCount:N0}/{r.TotalCount:N0} transactions");
    }
}
