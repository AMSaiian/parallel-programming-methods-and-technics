using System.CommandLine;
using Core;
using Core.Scenarios;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.IpcDemo;

public class IpcDemoRunner : BaseScenario
{
    private static readonly Option<string> MethodOption = new("--method", "-m")
    {
        Description = "IPC method: sharedmemory | namedpipe | tcp",
        Required = true,
        Validators =
        {
            r =>
            {
                var v = r.GetValueOrDefault<string>()?.ToLowerInvariant();
                if (v is not ("sharedmemory" or "namedpipe" or "tcp"))
                {
                    r.AddError("Method must be one of: sharedmemory, namedpipe, tcp.");
                }
            }
        }
    };

    public IpcDemoRunner()
        : base("ipc-demo", "IPC demo: C# main process to Java/MPJ supplementary process")
    {
        Options.Add(MethodOption);
        SetAction(RunAsync);
    }

    protected override void SetupEnvironment(ParseResult parseResult)
    {
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        Console.WriteLine($"Seed: {seed}");
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var method = parseResult.GetValue(MethodOption)!.ToLowerInvariant();
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);

        var javaDir = GetJavaDir();

        Console.WriteLine($"  method: {method}");
        Console.WriteLine($"  param:  {GetParam(method)}");

        var number = (long)new Random(seed).Next(1, 1_000_000);
        Console.WriteLine($"  number sent: {number}");

        (var elapsedMs, var received) = await ExecuteWithTimingAsync(() => method switch
        {
            "sharedmemory" => SharedMemoryIpc.RunAsync(number, javaDir, verbose),
            "namedpipe" => NamedPipeIpc.RunAsync(number, verbose),
            "tcp" => TcpSocketIpc.RunAsync(number, verbose),
            _ => throw new InvalidOperationException()
        });

        Console.WriteLine($"  number back: {received}");
        Console.WriteLine($"  round-trip: {(number == received ? "OK" : "MISMATCH")}");
        Console.WriteLine($"  elapsed: {elapsedMs} ms");
    }

    private static string GetParam(string method) => method switch
    {
        "sharedmemory" => SharedMemoryIpc.FileName,
        "namedpipe" => NamedPipeIpc.PipeName,
        "tcp" => TcpSocketIpc.Port.ToString(),
        _ => throw new InvalidOperationException()
    };

    private static string GetJavaDir()
    {
        var exeDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!;
        return Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "Java"));
    }
}
