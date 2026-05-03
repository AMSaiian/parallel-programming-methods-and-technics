using System.Diagnostics;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;

internal static class Race
{
    internal static async Task<(decimal TotalBefore, decimal TotalAfter, int Completed, int NegativeAccounts, int Corruptions)> RunAsync(
        BankAccount[] accounts,
        int workerCount,
        int durationMs)
    {
        var totalBefore = accounts.Sum(a => a.Balance);
        var completed = 0;
        var corruptions = 0;

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

                    try
                    {
                        var balance = from.Balance;
                        if (balance <= 0)
                        {
                            continue;
                        }

                        var amount = Math.Round((decimal)(Random.Shared.NextDouble() * (double)balance), 2);
                        if (amount <= 0)
                        {
                            continue;
                        }

                        from.Balance -= amount;
                        to.Balance += amount;
                        Interlocked.Increment(ref completed);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref corruptions);
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var totalAfter = 0m;
        var negativeAccounts = 0;
        foreach (var a in accounts)
        {
            try
            {
                totalAfter += a.Balance;
                if (a.Balance < 0)
                {
                    negativeAccounts++;
                }
            }
            catch (Exception)
            {
                corruptions++;
            }
        }

        return (totalBefore, totalAfter, completed, negativeAccounts, corruptions);
    }
}
