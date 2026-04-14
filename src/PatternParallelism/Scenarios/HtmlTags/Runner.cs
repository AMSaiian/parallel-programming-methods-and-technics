using System.CommandLine;
using Core;
using Core.Scenarios;

namespace PatternParallelism.Scenarios.HtmlTags;

public class Runner : BaseScenario
{
    public static readonly Option<string> DirectoryOption = new("--dir", "-d")
    {
        Description = "Path to directory containing HTML files (searched recursively)",
        Required = true,
    };

    public Runner() : base("tags", "Count HTML tag frequencies across a dataset of HTML files")
    {
        Options.Add(PatternOptions.Algorithm);
        Options.Add(DirectoryOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var algo = parseResult.GetValue(PatternOptions.Algorithm)!.ToLowerInvariant();
        var dir = parseResult.GetValue(DirectoryOption)!;
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);

        if (!Directory.Exists(dir))
        {
            Console.WriteLine($"  error: directory '{dir}' does not exist.");
            return;
        }

        var files = Directory
            .GetFiles(dir, "*.html", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(dir, "*.htm", SearchOption.AllDirectories))
            .ToArray();

        Console.WriteLine($"  found {files.Length} HTML files");

        (var ms, var tagCounts) = algo switch
        {
            "sequential" => await ExecuteWithTimingAsync(() => Sequential.Run(files)),
            "mapreduce" => await ExecuteWithTimingAsync(() => MapReduce.Run(files, threads)),
            "forkjoin" => await ExecuteWithTimingAsync(() => ForkJoin.Run(files, threads)),
            "workerpool" => await ExecuteWithTimingAsync(() => WorkerPool.Run(files, threads)),
            _ => throw new InvalidOperationException($"Unknown algorithm: {algo}")
        };

        Console.WriteLine($"  {algo}: {tagCounts.Count} distinct tags (took {ms} ms)");

        if (verbose)
        {
            foreach ((var tag, var count) in tagCounts.OrderByDescending(kv => kv.Value).Take(10))
            {
                Console.WriteLine($"    <{tag}>: {count}");
            }
        }
    }
}
