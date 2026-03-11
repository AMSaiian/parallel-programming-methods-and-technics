using System.CommandLine;

namespace Runner.Scenarios;

public class SimpleRunner : BaseScenario
{
    public SimpleRunner() : base("simple", "Run simple computation")
    {
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);
        await Task.CompletedTask;
    }
}
