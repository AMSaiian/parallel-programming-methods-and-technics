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

        Console.WriteLine($"  seeding {size:N0} doubles...");
        var data = Seeder.Seed(size, seed);

        (var ms, var stats) = algo switch
        {
            "sequential" => await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(data))),
            "mapreduce" => await ExecuteWithTimingAsync(() => MapReduce.Run(data, threads)),
            "forkjoin" => await ExecuteWithTimingAsync(() => ForkJoin.Run(data, threads)),
            "workerpool" => await ExecuteWithTimingAsync(() => WorkerPull.Run(data, threads)),
            _ => throw new InvalidOperationException($"Unknown algorithm: {algo}")
        };

        Console.WriteLine($"  {algo}: took {ms} ms");
        Console.WriteLine($"    min={stats.Min:F4}  max={stats.Max:F4}  mean={stats.Mean:F4}  median={stats.Median:F4}");
    }
}
