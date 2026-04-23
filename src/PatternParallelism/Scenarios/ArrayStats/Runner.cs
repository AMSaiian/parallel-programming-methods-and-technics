using System.CommandLine;
using Core;
using Core.Scenarios;

namespace PatternParallelism.Scenarios.ArrayStats;

public class Runner : BaseScenario
{
    public static readonly Option<int> SizeOption = new("--size", "-sz")
    {
        Description = "Number of elements (must be >= 1 000 000)",
        DefaultValueFactory = _ => 1_000_000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1_000_000)
                {
                    optionResult.AddError("Size must be at least 1 000 000.");
                }
            }
        }
    };

    public Runner() : base("stats", "Compute min, max, mean and median of a large random array")
    {
        Options.Add(PatternOptions.Algorithm);
        Options.Add(PatternOptions.WithSequential);
        Options.Add(SizeOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var algo = parseResult.GetValue(PatternOptions.Algorithm)!.ToLowerInvariant();
        var size = parseResult.GetValue(SizeOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var withSequential = parseResult.GetValue(PatternOptions.WithSequential);

        Console.WriteLine($"  seeding {size:N0} doubles...");
        var data = Common.Seed(size, seed);

        (var ms, var stats) = algo switch
        {
            "sequential" => await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(data))),
            "reducemap" => await ExecuteWithTimingAsync(() => MapReduce.Run(data, threads)),
            "forkjoin" => await ExecuteWithTimingAsync(() => ForkJoin.Run(data, threads)),
            "workerpool" => await ExecuteWithTimingAsync(() => WorkerPull.Run(data, threads)),
            _ => throw new InvalidOperationException($"Unknown algorithm: {algo}")
        };

        Console.WriteLine($"  {algo}: took {ms} ms");
        Console.WriteLine($"    min={stats.Min:F4}  max={stats.Max:F4}  mean={stats.Mean:F4}  median={stats.Median:F4}");

        if (algo != "sequential" && withSequential)
        {
            (var seqMs, var seqStats) = await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(data)));

            Console.WriteLine($"  sequential: took {seqMs} ms");
            Console.WriteLine($"    min={seqStats.Min:F4}  max={seqStats.Max:F4}  mean={seqStats.Mean:F4}  median={seqStats.Median:F4}");

            var speedup = (double)seqMs / ms;
            var efficiency = speedup / threads;
            Console.WriteLine($"  speedup={speedup:F2}x  efficiency={efficiency:F4}");
        }
    }
}
