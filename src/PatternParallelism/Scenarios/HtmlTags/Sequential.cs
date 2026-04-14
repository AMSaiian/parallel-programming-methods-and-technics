namespace PatternParallelism.Scenarios.HtmlTags;

internal static class Sequential
{
    internal static Task<Dictionary<string, int>> Run(string[] files)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            Parser.Merge(result, Parser.ParseFile(file));
        }
        return Task.FromResult(result);
    }
}
