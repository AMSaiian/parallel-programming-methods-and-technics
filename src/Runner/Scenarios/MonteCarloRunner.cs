using System.CommandLine;

namespace Runner.Scenarios;

public static class MonteCarloRunner
{
    public static readonly Command Command = new("monte", "Run MonteCarlo simulation");

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

    static MonteCarloRunner()
    {
        Command.Options.Add(IterationsOption);
        Command.SetAction(RunAsync);
    }

    private static async Task RunAsync(ParseResult parseResult)
    {
        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var iterations = parseResult.GetValue(IterationsOption);

        // TODO: Implement MonteCarlo runner
        Console.WriteLine("MonteCarlo runner - will be implemented soon");
        Console.WriteLine($"  threads:    {threads}");
        Console.WriteLine($"  iterations: {iterations}");
        await Task.CompletedTask;
    }
}
