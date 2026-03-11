using System.CommandLine;

namespace Runner.Scenarios;

public static class SimpleRunner
{
    public static readonly Command Command = new("simple", "Run Simple actions");

    static SimpleRunner()
    {
        Command.SetAction(RunAsync);
    }

    private static async Task RunAsync(ParseResult parseResult)
    {
        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);

        // TODO: Implement Simple runner
        Console.WriteLine("Simple runner - will be implemented soon");
        Console.WriteLine($"  threads: {threads}");
        await Task.CompletedTask;
    }
}
