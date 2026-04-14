using System.Text.RegularExpressions;

namespace PatternParallelism.Scenarios.HtmlTags;

internal static class Parser
{
    private static readonly Regex TagRegex =
        new(@"</?(\w[\w\d]*)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static Dictionary<string, int> ParseFile(string path)
    {
        var html = File.ReadAllText(path);
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in TagRegex.Matches(html))
        {
            var tag = m.Groups[1].Value.ToLowerInvariant();
            counts[tag] = counts.GetValueOrDefault(tag) + 1;
        }
        return counts;
    }

    internal static void Merge(Dictionary<string, int> into,
                               Dictionary<string, int> from)
    {
        foreach ((var tag, var count) in from)
        {
            into[tag] = into.GetValueOrDefault(tag) + count;
        }
    }
}
