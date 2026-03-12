using System.CommandLine;

namespace Runner.Scenarios;

public class MonteCarloRunner : BaseScenario
{
    public static readonly Option<int> IterationsOption = new("--iterations", "-i")
    {
        Description = "Number of iterations",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1)
                {
                    optionResult.AddError("Iterations must be greater than 0.");
                }
            }
        }
    };

    public MonteCarloRunner() : base("monte", "Run MonteCarlo simulation")
    {
        Options.Add(IterationsOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var iterations = parseResult.GetValue(IterationsOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);

        var (seqMs, seqPi) = await ExecuteWithTimingAsync(() => Task.FromResult(RunSequential(iterations, seed)));
        Console.WriteLine($"  sequential: took {seqMs} ms");
        if (verbose)
        {
            Console.WriteLine($"    π ≈ {seqPi}");
        }

        var (parMs, parPi) = await ExecuteWithTimingAsync(() => RunParallel(iterations, threads, seed));
        Console.WriteLine($"  parallel:   took {parMs} ms");
        if (verbose)
        {
            Console.WriteLine($"    π ≈ {parPi}");
        }
    }

    private static double RunSequential(int iterations, int seed)
    {
        var random = new Random(seed);
        var insideCircle = 0;

        for (var i = 0; i < iterations; i++)
        {
            var x = random.NextDouble();
            var y = random.NextDouble();

            if (x * x + y * y <= 1.0)
            {
                insideCircle++;
            }
        }

        return 4.0 * insideCircle / iterations;
    }

    private static async Task<double> RunParallel(int iterations, int threads, int seed)
    {
        var chunkSize = iterations / threads;
        var counts = new int[threads];

        var tasks = Enumerable
            .Range(0, threads)
            .Select(t => Task.Run(() =>
            {
                var localRandom = new Random(seed + t);
                var localCount = 0;
                var start = t * chunkSize;
                int end;
                if (t == threads - 1)
                {
                    end = iterations;
                }
                else
                {
                    end = start + chunkSize;
                }

                for (var i = start; i < end; i++)
                {
                    var x = localRandom.NextDouble();
                    var y = localRandom.NextDouble();

                    if (x * x + y * y <= 1.0)
                    {
                        localCount++;
                    }
                }

                counts[t] = localCount;
            }));

        await Task.WhenAll(tasks);

        return 4.0 * counts.Sum() / iterations;
    }
}
