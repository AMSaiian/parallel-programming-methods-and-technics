using System.CommandLine;
using Core;
using Core.Scenarios;

namespace PatternParallelism.Scenarios.MatMul;

public class Runner : BaseScenario
{
    public static readonly Option<int> DimOption = new("--dim", "-d")
    {
        Description = "Matrix dimension N — multiplies two N×N matrices",
        DefaultValueFactory = _ => 1000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 2)
                {
                    optionResult.AddError("Dimension must be at least 2.");
                }
            }
        }
    };

    public Runner() : base("matmul", "Multiply two large N×N matrices")
    {
        Options.Add(PatternOptions.Algorithm);
        Options.Add(PatternOptions.WithSequential);
        Options.Add(DimOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var algo = parseResult.GetValue(PatternOptions.Algorithm)!.ToLowerInvariant();
        var n = parseResult.GetValue(DimOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);
        var withSequential = parseResult.GetValue(PatternOptions.WithSequential);

        Console.WriteLine($"  seeding two {n}×{n} matrices (seed={seed})...");
        (var a, var b) = Seeder.Seed(n, seed);

        (var ms, var c) = algo switch
        {
            "sequential" => await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(a, b, n))),
            "reducemap" => await ExecuteWithTimingAsync(() => MapReduce.Run(a, b, n, threads)),
            "forkjoin" => await ExecuteWithTimingAsync(() => ForkJoin.Run(a, b, n, threads)),
            "workerpool" => await ExecuteWithTimingAsync(() => WorkerPool.Run(a, b, n, threads)),
            _ => throw new InvalidOperationException($"Unknown algorithm: {algo}")
        };

        Console.WriteLine($"  {algo}: took {ms} ms");

        var expected = 0.0;
        for (var k = 0; k < n; k++)
        {
            expected += a[0, k] * b[k, 0];
        }
        var diff = Math.Abs(c[0, 0] - expected);
        Console.WriteLine($"  assertion C[0,0]: expected={expected:F6}  actual={c[0, 0]:F6}  delta={diff:E2}  ok={diff < 1e-6}");

        if (verbose)
        {
            Console.WriteLine("  top-left 3×3 corner of C:");
            for (var i = 0; i < Math.Min(3, n); i++)
            {
                var row = string.Join("  ", Enumerable.Range(0, Math.Min(3, n)).Select(j => $"{c[i, j]:F4}"));
                Console.WriteLine($"    [ {row} ]");
            }
        }

        if (algo != "sequential" && withSequential)
        {
            (var seqMs, _) = await ExecuteWithTimingAsync(() => Task.FromResult(Sequential.Run(a, b, n)));

            Console.WriteLine($"  sequential: took {seqMs} ms");

            var speedup = (double)seqMs / ms;
            var efficiency = speedup / threads;
            Console.WriteLine($"  speedup={speedup:F2}x  efficiency={efficiency:F4}");
        }
    }
}
