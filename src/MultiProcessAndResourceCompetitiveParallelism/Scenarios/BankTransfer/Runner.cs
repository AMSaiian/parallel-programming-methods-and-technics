using System.CommandLine;
using Core;
using Core.Scenarios;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;

public class BankTransferRunner : BaseScenario
{
    private static readonly Option<string> ScenarioOption = new("--scenario", "-sc")
    {
        Description = "Scenario to run: race | deadlock | safe",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<string>()?.ToLowerInvariant();
                if (value is not ("race" or "deadlock" or "safe"))
                {
                    optionResult.AddError("Scenario must be one of: race, deadlock, safe.");
                }
            }
        }
    };

    private static readonly Option<int> AccountsOption = new("--accounts", "-ac")
    {
        Description = "Number of bank accounts to create (>= 100)",
        DefaultValueFactory = _ => 200,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 100)
                {
                    optionResult.AddError("Accounts must be at least 100.");
                }
            }
        }
    };

    private static readonly Option<int> WorkersOption = new("--workers", "-w")
    {
        Description = "Number of concurrent transfer worker tasks (>= 1000)",
        DefaultValueFactory = _ => 1000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1000)
                {
                    optionResult.AddError("Workers must be at least 1000.");
                }
            }
        }
    };

    private static readonly Option<int> DurationOption = new("--duration", "-d")
    {
        Description = "How long to run transfers in seconds",
        DefaultValueFactory = _ => 5,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1)
                {
                    optionResult.AddError("Duration must be at least 1 second.");
                }
            }
        }
    };

    public BankTransferRunner() : base("bank-transfer", "Bank account transfers: demonstrates race condition, deadlock, and safe synchronisation")
    {
        Options.Add(ScenarioOption);
        Options.Add(AccountsOption);
        Options.Add(WorkersOption);
        Options.Add(DurationOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var scenario = parseResult.GetValue(ScenarioOption)!.ToLowerInvariant();
        var accounts = parseResult.GetValue(AccountsOption);
        var workers = parseResult.GetValue(WorkersOption);
        var duration = parseResult.GetValue(DurationOption);
        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);
        var durationMs = duration * 1_000;

        Console.WriteLine($"  accounts={accounts}  workers={workers}  duration={duration}s  scenario={scenario}");

        var bankAccounts = Common.CreateAccounts(accounts, seed);

        switch (scenario)
        {
            case "race":
                await RunRaceAsync(bankAccounts, workers, durationMs, verbose);
                break;
            case "deadlock":
                await RunDeadlockAsync(bankAccounts, workers, durationMs, verbose);
                break;
            case "safe":
                await RunSafeAsync(bankAccounts, workers, durationMs, verbose);
                break;
        }
    }

    private static async Task RunRaceAsync(BankAccount[] accounts, int workers, int durationMs, bool verbose)
    {
        (var elapsed, var result) = await ExecuteWithTimingAsync(
            () => Race.RunAsync(accounts, workers, durationMs));

        var (before, after, completed, negative, corruptions) = result;
        var drift = after - before;

        Console.WriteLine($"  completed transfers : {completed:N0}");
        Console.WriteLine($"  memory corruptions  : {corruptions:N0} ");
        Console.WriteLine($"  total before        : {before:F2}");
        Console.WriteLine($"  total after         : {after:F2}");
        Console.WriteLine($"  drift (created/lost): {drift:+0.00;-0.00;0.00}");
        Console.WriteLine($"  accounts with negative balance: {negative}");
        Console.WriteLine($"  elapsed             : {elapsed} ms");

        if (verbose)
        {
            PrintSampleBalances(accounts, 10);
        }
    }

    private static async Task RunDeadlockAsync(BankAccount[] accounts, int workers, int durationMs, bool verbose)
    {
        (var elapsed, var result) = await ExecuteWithTimingAsync(
            () => Deadlock.RunAsync(accounts, workers, durationMs));

        var (before, after, completed, deadlocks) = result;
        var ratio = completed + deadlocks > 0
            ? (double)deadlocks / (completed + deadlocks) * 100
            : 0;

        Console.WriteLine($"  completed transfers : {completed:N0}");
        Console.WriteLine($"  deadlocks detected  : {deadlocks:N0}  ← {ratio:F3}% of all lock attempts");
        Console.WriteLine($"  total before        : {before:F2}");
        Console.WriteLine($"  total after         : {after:F2}");
        Console.WriteLine($"  elapsed             : {elapsed} ms");

        if (verbose)
        {
            PrintSampleBalances(accounts, 10);
        }
    }

    private static async Task RunSafeAsync(BankAccount[] accounts, int workers, int durationMs, bool verbose)
    {
        (var elapsed, var result) = await ExecuteWithTimingAsync(
            () => Safe.RunAsync(accounts, workers, durationMs));

        var (before, after, completed) = result;
        var drift = after - before;

        Console.WriteLine($"  completed transfers : {completed:N0}");
        Console.WriteLine($"  total before        : {before:F2}");
        Console.WriteLine($"  total after         : {after:F2}");
        Console.WriteLine($"  drift               : {drift:+0.00;-0.00;0.00}.   must be 0");
        Console.WriteLine($"  accounts with negative balance: {accounts.Count(a => a.Balance < 0)}.   must be 0");
        Console.WriteLine($"  elapsed             : {elapsed} ms");

        if (verbose)
        {
            PrintSampleBalances(accounts, 10);
        }
    }

    private static void PrintSampleBalances(BankAccount[] accounts, int count)
    {
        Console.WriteLine($"  sample balances (first {count}):");
        foreach (var a in accounts.Take(count))
        {
            Console.WriteLine($"    account[{a.Id,3}]: {a.Balance,12:F2}");
        }
    }
}
