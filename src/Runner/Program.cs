using System.CommandLine;
using Runner.Scenarios;

namespace Runner;

public static class Program
{
    public static RootCommand RootCommand { get; } = new("Parallel Programming Methods Runner");

    public static readonly IReadOnlyList<Command> Commands =
    [
        new MonteCarloRunner(),
        new FactorRunner(),
        new SimpleRunner(),
    ];

    public static async Task<int> Main(string[] args)
    {
        foreach (var command in Commands)
        {
            RootCommand.Subcommands.Add(command);
        }

        return await RootCommand
            .Parse(args)
            .InvokeAsync();
    }
}
