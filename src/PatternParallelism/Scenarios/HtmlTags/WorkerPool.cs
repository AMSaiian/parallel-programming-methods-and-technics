using System.Threading.Channels;

namespace PatternParallelism.Scenarios.HtmlTags;

internal static class WorkerPool
{
    internal static async Task<Dictionary<string, int>> Run(string[] files,
                                                            int threads)
    {
        var channel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions { SingleWriter = true });

        var workerDicts = Enumerable.Range(0, threads)
            .Select(_ => new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
            .ToArray();

        var workers = Enumerable.Range(0, threads).Select(t => Task.Run(async () =>
        {
            await foreach (var file in channel.Reader.ReadAllAsync())
            {
                Parser.Merge(workerDicts[t], Parser.ParseFile(file));
            }
        })).ToArray();

        foreach (var file in files)
        {
            await channel.Writer.WriteAsync(file);
        }
        channel.Writer.Complete();

        await Task.WhenAll(workers);

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in workerDicts)
        {
            Parser.Merge(result, d);
        }
        return result;
    }
}
