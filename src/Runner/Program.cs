using System.CommandLine;
using Runner.Scenarios;

namespace Runner;

public static class Program
{
    public static RootCommand RootCommand { get; } = new("Parallel Programming Methods Runner");

    public static readonly IReadOnlyList<Command> Commands =
    [
        MonteCarloRunner.Command,
        FactorRunner.Command,
        SimpleRunner.Command,
    ];

    public static async Task<int> Main(string[] args)
    {
        RootCommand.Options.Add(GlobalOptions.ThreadsOption);

        foreach (var command in Commands)
        {
            RootCommand.Subcommands.Add(command);
        }

        var parseResult = RootCommand.Parse(args);

        return await parseResult
            .InvokeAsync();
    }
}
