namespace PatternParallelism.Scenarios.HtmlTags;

internal static class ForkJoin
{
    internal static Task<Dictionary<string, int>> Run(string[] files,
                                                      int threads)
    {
        var depth = (int)Math.Ceiling(Math.Log2(Math.Max(1, threads)));
        return Core(files, 0, files.Length, depth);
    }

    private static async Task<Dictionary<string, int>> Core(string[] files,
                                                            int start,
                                                            int end,
                                                            int depth)
    {
        if (depth == 0 || end - start <= 1)
        {
            var local = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = start; i < end; i++)
            {
                Parser.Merge(local, Parser.ParseFile(files[i]));
            }
            return local;
        }

        var mid = (start + end) / 2;
        var left = Task.Run(() => Core(files, start, mid, depth - 1));
        var right = Task.Run(() => Core(files, mid, end, depth - 1));
        var results = await Task.WhenAll(left, right);
        Parser.Merge(results[0], results[1]);
        return results[0];
    }
}
