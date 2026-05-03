using System.CommandLine;
using MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;
using MultiProcessAndResourceCompetitiveParallelism.Scenarios.IpcDemo;

namespace MultiProcessAndResourceCompetitiveParallelism;

public static class Commands
{
    public static IReadOnlyList<Command> All { get; } =
    [
        new BankTransferRunner(),
        new IpcDemoRunner(),
    ];
}
