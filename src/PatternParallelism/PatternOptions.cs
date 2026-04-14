using System.CommandLine;

namespace PatternParallelism;

public static class PatternOptions
{
    public static readonly Option<string> Algorithm = new("--algo", "-a")
    {
        Description = "Parallel pattern: sequential | mapreduce | forkjoin | workerpool",
        Required = true,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<string>()?.ToLowerInvariant();
                if (value is not ("sequential" or "mapreduce" or "forkjoin" or "workerpool"))
                {
                    optionResult.AddError("Algorithm must be one of: sequential, mapreduce, forkjoin, workerpool.");
                }
            }
        }
    };

    public static readonly Option<bool> WithSequential = new("--seq", "-sq")
    {
        Description = "Usage sequential",
        DefaultValueFactory = _ => false,
        Arity = ArgumentArity.ZeroOrOne,
    };
}
