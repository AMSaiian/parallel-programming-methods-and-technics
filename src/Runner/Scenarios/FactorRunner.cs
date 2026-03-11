using System.CommandLine;

namespace Runner.Scenarios;

public static class FactorRunner
{
    public static readonly Command Command = new("factor", "Run Factor computation");

    public static readonly Option<int> SizeOption = new("--amount", "-a")
    {
        Description = "Amount of input",
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

    static FactorRunner()
    {
        Command.Options.Add(SizeOption);
        Command.SetAction(RunAsync);
    }

    private static async Task RunAsync(ParseResult parseResult)
    {
        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var size = parseResult.GetValue(SizeOption);

        // TODO: Implement Factor runner
        Console.WriteLine("Factor runner - will be implemented soon");
        IntroduceParams(threads, size);

        await Task.CompletedTask;
    }

    private static void IntroduceParams(int threads, int size)
    {
        Console.WriteLine($"  threads: {threads}");
        Console.WriteLine($"  amount:  {size}");
    }
} 
