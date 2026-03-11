using System.CommandLine;

namespace Runner.Scenarios;

public static class BaseScenario
{
    public static void SetupEnvironment(ParseResult parseResult)
    {
        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);

        ThreadPool.SetMinThreads(threads, threads);
        ThreadPool.SetMaxThreads(threads, threads);
        ThreadPool.GetMinThreads(out var minThreads, out _);

        Console.WriteLine($"Environment setup: threads = {minThreads}");
    }
}