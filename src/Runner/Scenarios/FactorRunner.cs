using System.CommandLine;
using System.Numerics;

namespace Runner.Scenarios;

public class FactorRunner : BaseScenario
{
    public static readonly Option<int> SizeOption = new("--member", "-m")
    {
        Description = "Amount of factorials to compute (computes 1! through amount!)",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1)
                {
                    optionResult.AddError("Amount must be greater than 0.");
                }
            }
        }
    };

    public FactorRunner() : base("factor", "Run Factor computation")
    {
        Options.Add(SizeOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var amount = parseResult.GetValue(SizeOption);

        var (seqMs, seqResult) = await ExecuteWithTimingAsync(() => Task.FromResult(RunSequential(amount)));
        Console.WriteLine($"  sequential: computed {amount} factorial (took {seqMs} ms)");
        // Console.WriteLine($"  {amount}! = {seqResults[^1]}");

        var (parMs, parResult) = await ExecuteWithTimingAsync(() => RunParallel(amount, threads));
        Console.WriteLine($"  parallel:   computed {amount} factorial (took {parMs} ms)");
        // Console.WriteLine($"  {amount}! = {parResults[^1]}");
    }

    private static BigInteger RunSequential(int amount)
    {
        BigInteger result = BigInteger.One;

        for (int number = 2; number <= amount; number++)
        {
            result *= number;
        }

        return result;
    }

    private static async Task<BigInteger> RunParallel(int amount, int threadCount)
    {
        int chunkSize = amount / threadCount;
        var chunkProducts = new BigInteger[threadCount];

        var tasks = Enumerable
            .Range(0, threadCount)
            .Select(threadIndex => Task.Run(() =>
            {
                int chunkStart = threadIndex * chunkSize + 1;
                int chunkEnd = (threadIndex == threadCount - 1) ? amount : (threadIndex + 1) * chunkSize;

                BigInteger product = BigInteger.One;
                for (int number = chunkStart; number <= chunkEnd; number++)
                {
                    product *= number;
                }

                chunkProducts[threadIndex] = product;
            }));

        await Task.WhenAll(tasks);

        BigInteger result = BigInteger.One;
        for (int i = 0; i < threadCount; i++) 
        {
            result *= chunkProducts[i];
        }

        return result;
    }
}
