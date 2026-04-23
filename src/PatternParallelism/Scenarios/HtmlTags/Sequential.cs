namespace PatternParallelism.Scenarios.HtmlTags;

internal static class Sequential
{
    internal static async Task<Dictionary<string, int>> Run(string[] files)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            Parser.Merge(result, await Parser.ParseFileAsync(file));
        }
        return result;
    }
}
