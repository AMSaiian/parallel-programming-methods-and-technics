using System.Diagnostics;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;

internal static class Safe
{
    internal static async Task<(decimal TotalBefore, decimal TotalAfter, int Completed)> RunAsync(
        BankAccount[] accounts,
        int workerCount,
        int durationMs)
    {
        var totalBefore = accounts.Sum(a => a.Balance);
        var completed = 0;

        var sw = Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, workerCount)
            .Select(_ => Task.Run(() =>
            {
                while (sw.ElapsedMilliseconds < durationMs)
                {
                    var fromIdx = Random.Shared.Next(accounts.Length);
                    var toIdx = Random.Shared.Next(accounts.Length);
                    if (fromIdx == toIdx)
                    {
                        continue;
                    }

                    var from = accounts[fromIdx];
                    var to = accounts[toIdx];

                    var first = from.Id < to.Id ? from : to;
                    var second = from.Id < to.Id ? to : from;

                    lock (first.LockObj)
                    {
                        lock (second.LockObj)
                        {
                            if (from.Balance > 0)
                            {
                                var amount = Math.Round((decimal)(Random.Shared.NextDouble() * (double)from.Balance), 2);
                                if (amount > 0)
                                {
                                    from.Balance -= amount;
                                    to.Balance += amount;
                                    Interlocked.Increment(ref completed);
                                }
                            }
                        }
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var totalAfter = accounts.Sum(a => a.Balance);
        return (totalBefore, totalAfter, completed);
    }
}
