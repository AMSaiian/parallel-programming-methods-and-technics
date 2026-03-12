using System.CommandLine;
using System.Text;

namespace Runner.Scenarios;

public class WordCountRunner : BaseScenario
{
    private static readonly string[] WordPool =
    [
        "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog",
        "parallel", "sequential", "thread", "task", "async", "await",
        "compute", "process", "file", "directory", "count", "word",
        "lorem", "ipsum", "dolor", "sit", "amet", "consectetur",
        "programming", "algorithm", "performance", "benchmark"
    ];

    public static readonly Option<int> FileCountOption = new("--files", "-fc")
    {
        Description = "Number of text files to generate (max 10000)",
        DefaultValueFactory = _ => 1000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value is < 1 or > 10000)
                {
                    optionResult.AddError("File count must be between 1 and 10000.");
                }
            }
        }
    };

    public static readonly Option<int> WordsPerFileOption = new("--words", "-w")
    {
        Description = "Approximate number of words per file (max 10000)",
        DefaultValueFactory = _ => 500,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value is < 1 or > 10000)
                {
                    optionResult.AddError("Words per file must be between 1 and 10000.");
                }
            }
        }
    };

    public static readonly Option<int> FolderCountOption = new("--folders", "-fd")
    {
        Description = "Number of subdirectories to create (max 10000)",
        DefaultValueFactory = _ => 100,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value is < 1 or > 10000)
                {
                    optionResult.AddError("Folder count must be between 1 and 10000.");
                }
            }
        }
    };

    public static readonly Option<bool> TeardownOption = new("--teardown", "-td")
    {
        Description = "Preserve generated files after run (default: false)",
        DefaultValueFactory = _ => false,
    };

    public WordCountRunner() : base("words", "Recursively count words in randomly generated text files")
    {
        Options.Add(FileCountOption);
        Options.Add(WordsPerFileOption);
        Options.Add(FolderCountOption);
        Options.Add(TeardownOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);
        var fileCount = parseResult.GetValue(FileCountOption);
        var wordsPerFile = parseResult.GetValue(WordsPerFileOption);
        var folderCount = parseResult.GetValue(FolderCountOption);
        var teardown = parseResult.GetValue(TeardownOption);

        var workDir = Path.Combine(Path.GetTempPath(), $"wordcount_{seed}");

        try
        {
            Console.WriteLine($"  generating {fileCount} files (max {wordsPerFile} words each) across {folderCount} folders in {workDir}");
            GenerateFiles(workDir, fileCount, wordsPerFile, folderCount, seed);

            var files = Directory.GetFiles(workDir, "*.txt", SearchOption.AllDirectories);
            if (verbose)
            {
                Console.WriteLine($"  found {files.Length} .txt files across directory tree");
            }

            var (seqMs, seqCount) = await ExecuteWithTimingAsync(() => Task.FromResult(CountWordsSequential(files)));
            Console.WriteLine($"  sequential: {seqCount} words (took {seqMs} ms)");

            var (parMs, parCount) = await ExecuteWithTimingAsync(() => CountWordsParallel(files, threads));
            Console.WriteLine($"  parallel:   {parCount} words (took {parMs} ms)");
        }
        finally
        {
            if (teardown)
            {
                Console.WriteLine($"  preserved: {workDir}");
            }
            else if (Directory.Exists(workDir))
            {
                Directory.Delete(workDir, recursive: true);
                Console.WriteLine($"  cleaned up {workDir}");
            }
        }
    }

    private static void GenerateFiles(string rootDir, int fileCount, int wordsPerFile, int folderCount, int seed)
    {
        if (Directory.Exists(rootDir))
        {
            Directory.Delete(rootDir, recursive: true);
        }

        Directory.CreateDirectory(rootDir);

        var rng = new Random(seed);

        var dirs = new List<string>(folderCount + 1) { rootDir };
        for (var d = 0; d < folderCount; d++)
        {
            var parent = dirs[rng.Next(dirs.Count)];
            var sub = Path.Combine(parent, $"dir_{d:D5}");
            Directory.CreateDirectory(sub);
            dirs.Add(sub);
        }

        for (var f = 0; f < fileCount; f++)
        {
            var dir = dirs[rng.Next(dirs.Count)];
            var path = Path.Combine(dir, $"file_{f:D6}.txt");
            var sb = new StringBuilder();

            var count = rng.Next(0, wordsPerFile + 1);
            for (var w = 0; w < count; w++)
            {
                sb.Append(WordPool[rng.Next(WordPool.Length)]);
                sb.Append(w % 10 == 9 ? '\n' : ' ');
            }

            File.WriteAllText(path, sb.ToString());
        }
    }

    private static long CountWordsSequential(string[] files)
    {
        var total = 0L;

        foreach (var file in files)
        {
            total += CountWordsInFile(file);
        }

        return total;
    }

    private static async Task<long> CountWordsParallel(string[] files, int threads)
    {
        var chunkSize = files.Length / threads;
        var counts = new long[threads];

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var start = t * chunkSize;
            int end;
            if (t == threads - 1)
            {
                end = files.Length;
            }
            else
            {
                end = start + chunkSize;
            }

            var localCount = 0L;
            for (var i = start; i < end; i++)
            {
                localCount += CountWordsInFile(files[i]);
            }

            counts[t] = localCount;
        }));

        await Task.WhenAll(tasks);

        return counts.Sum();
    }

    private static long CountWordsInFile(string path)
    {
        var count = 0L;
        var inWord = false;

        foreach (var ch in File.ReadAllText(path))
        {
            if (char.IsWhiteSpace(ch))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }
}
