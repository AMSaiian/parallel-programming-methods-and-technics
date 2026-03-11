using System.CommandLine;

namespace Runner;

public static class GlobalOptions
{
    public static readonly Option<int> ThreadsOption = new("--threads", "-t")
    {
        Description = "Number of threads",
        DefaultValueFactory = _ => 1,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 1)
                {
                    optionResult.AddError("Threads must be greater than 0.");
                }
            }
        }
    };
}
