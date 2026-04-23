using System.CommandLine;

namespace PatternParallelism;

public static class Commands
{
    public static IReadOnlyList<Command> All { get; } =
    [
        new Scenarios.HtmlTags.Runner(),
        new Scenarios.ArrayStats.Runner(),
        new Scenarios.MatMul.Runner(),
        new Scenarios.Transactions.Runner(),
    ];
}
