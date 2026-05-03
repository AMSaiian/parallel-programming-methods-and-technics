using System.CommandLine;

namespace Runner;

public static class Program
{
    public static RootCommand RootCommand { get; } = new("Parallel Programming Methods Runner");

    public static readonly IReadOnlyList<Command> Commands =
    [
        ..PrimitiveParallelism.Commands.All,
        ..PatternParallelism.Commands.All,
        ..MultiProcessAndResourceCompetitiveParallelism.Commands.All,
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
