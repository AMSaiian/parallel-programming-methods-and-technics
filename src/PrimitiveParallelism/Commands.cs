using System.CommandLine;
using PrimitiveParallelism.Scenarios;

namespace PrimitiveParallelism;

public static class Commands
{
    public static IReadOnlyList<Command> All { get; } =
    [
        new MonteCarloRunner(),
        new FactorRunner(),
        new SimpleRunner(),
        new MatrixTransposeRunner(),
        new WordCountRunner(),
    ];
}
