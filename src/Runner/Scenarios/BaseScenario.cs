using System.CommandLine;
using System.Diagnostics;

namespace Runner.Scenarios;

public abstract class BaseScenario : Command
{
    protected BaseScenario(string name, string description) : base(name, description)
    {
        Options.Add(GlobalOptions.ThreadsOption);
        Options.Add(GlobalOptions.SeedOption);
        Options.Add(GlobalOptions.VerboseOption);
    }

    protected virtual void SetupEnvironment(ParseResult parseResult)
    {
        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        ThreadPool.SetMinThreads(threads, threads);
        ThreadPool.SetMaxThreads(threads, threads);
        ThreadPool.GetMinThreads(out var minThreads, out _);

        Console.WriteLine($"Environment setup: threads = {minThreads}");
        Console.WriteLine($"Seed: {seed}");
    }

    protected virtual Task RunAsync(ParseResult parseResult)
    {
        SetupEnvironment(parseResult);
        return Task.CompletedTask;
    }

    protected static async Task<(long ElapsedMs, T Result)> ExecuteWithTimingAsync<T>(Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await action();
        stopwatch.Stop();

        return (stopwatch.ElapsedMilliseconds, result);
    }
}
