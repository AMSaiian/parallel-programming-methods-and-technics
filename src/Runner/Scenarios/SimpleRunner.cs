using System.CommandLine;

namespace Runner.Scenarios;

public class SimpleRunner : BaseScenario
{
    public static readonly Option<int> LowerOption = new("--lower", "-lo")
    {
        Description = "Lower bound for prime search (inclusive)",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 2)
                {
                    optionResult.AddError("Lower bound must be at least 2.");
                }
            }
        }
    };

    public static readonly Option<int> UpperOption = new("--upper", "-up")
    {
        Description = "Upper bound for prime search (inclusive)",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 2)
                {
                    optionResult.AddError("Upper bound must be at least 2.");
                }
            }
        }
    };

    public SimpleRunner() : base("simple", "Find prime numbers within a given range")
    {
        Options.Add(LowerOption);
        Options.Add(UpperOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var lower = parseResult.GetValue(LowerOption);
        var upper = parseResult.GetValue(UpperOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);

        if (lower > upper)
        {
            Console.WriteLine("Error: lower bound must be less than or equal to upper bound.");
            return;
        }

        var (seqMs, seqPrimes) = await ExecuteWithTimingAsync(() => Task.FromResult(RunSequential(lower, upper)));
        Console.WriteLine($"  sequential: found {seqPrimes.Count} primes in [{lower}, {upper}] (took {seqMs} ms)");
        if (verbose && seqPrimes.Count > 0)
        {
            Console.WriteLine($"    largest prime: {seqPrimes[^1]}");
        }

        var (parMs, parPrimes) = await ExecuteWithTimingAsync(() => RunParallel(lower, upper, threads));
        Console.WriteLine($"  parallel:   found {parPrimes.Count} primes in [{lower}, {upper}] (took {parMs} ms)");
        if (verbose && parPrimes.Count > 0)
        {
            Console.WriteLine($"    largest prime: {parPrimes[^1]}");
        }
    }

    private static List<int> RunSequential(int lower, int upper)
    {
        var primes = new List<int>();

        for (var n = lower; n <= upper; n++)
        {
            if (IsPrime(n))
            {
                primes.Add(n);
            }
        }

        return primes;
    }

    private static async Task<List<int>> RunParallel(int lower, int upper, int threads)
    {
        var rangeSize = upper - lower + 1;
        var chunkSize = rangeSize / threads;
        var chunkResults = new List<int>[threads];

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var start = lower + t * chunkSize;
            int end;
            if (t == threads - 1)
            {
                end = upper;
            }
            else
            {
                end = start + chunkSize - 1;
            }

            var localPrimes = new List<int>();

            for (var n = start; n <= end; n++)
            {
                if (IsPrime(n))
                {
                    localPrimes.Add(n);
                }
            }

            chunkResults[t] = localPrimes;
        }));

        await Task.WhenAll(tasks);

        var combined = new List<int>(rangeSize);
        foreach (var chunk in chunkResults)
        {
            combined.AddRange(chunk);
        }

        return combined;
    }

    private static bool IsPrime(int n)
    {
        if (n < 2)
        {
            return false;
        }

        if (n == 2)
        {
            return true;
        }

        if (n % 2 == 0)
        {
            return false;
        }

        var sqrt = (int)Math.Sqrt(n);
        for (var i = 3; i <= sqrt; i += 2)
        {
            if (n % i == 0)
            {
                return false;
            }
        }

        return true;
    }
}
