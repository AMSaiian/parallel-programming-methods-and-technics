namespace PatternParallelism.Scenarios.HtmlTags;

internal static class MapReduce
{
    internal static async Task<Dictionary<string, int>> Run(string[] files,
                                                            int threads)
    {
        var chunkSize = (int)Math.Ceiling(files.Length / (double)threads);

        var mapTasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var start = t * chunkSize;
            var end = Math.Min(start + chunkSize, files.Length);
            var local = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = start; i < end; i++)
            {
                Parser.Merge(local, Parser.ParseFile(files[i]));
            }
            return local;
        }));

        var partials = await Task.WhenAll(mapTasks);

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var partial in partials)
        {
            Parser.Merge(result, partial);
        }
        return result;
    }
}
