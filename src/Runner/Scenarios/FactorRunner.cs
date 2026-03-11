using System.CommandLine;

namespace Runner.Scenarios;

public class FactorRunner : BaseScenario
{
    public static readonly Option<int> SizeOption = new("--amount", "-a")
    {
        Description = "Amount of input",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1)
                {
                    optionResult.AddError("Amount must be greater than 0.");
                }
            }
        }
    };

    public FactorRunner() : base("factor", "Run Factor computation")
    {
        Options.Add(SizeOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var size = parseResult.GetValue(SizeOption);

        // TODO: Implement Factor runner
        Console.WriteLine("Factor runner - will be implemented soon");
        Console.WriteLine($"  amount:  {size}");
        await Task.CompletedTask;
    }
}
