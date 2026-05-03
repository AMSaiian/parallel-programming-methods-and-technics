using System.Diagnostics;

namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;

// Deadlock: locks are acquired in an arbitrary (non-deterministic) order.
//
// Problem pattern (two threads with overlapping account pairs):
//   Thread A picks (from=accounts[5], to=accounts[3]):
//     lock(accounts[5].LockObj)          ← acquired
//       waits for lock(accounts[3].LockObj)  ← BLOCKED — held by Thread B
//
//   Thread B picks (from=accounts[3], to=accounts[5]):
//     lock(accounts[3].LockObj)          ← acquired
//       waits for lock(accounts[5].LockObj)  ← BLOCKED — held by Thread A
//
//   Both threads wait forever — DEADLOCK.
//
// Detection: Monitor.TryEnter with a timeout is used to detect (not prevent) deadlock.
// When the second lock cannot be acquired within the timeout the transfer is aborted
// and the deadlock event is counted.  In a real unsynchronised system this would hang.

internal static class Deadlock
{
    private const int LockTimeoutMs = 300;

    internal static async Task<(decimal TotalBefore, decimal TotalAfter, int Completed, int DeadlocksDetected)> RunAsync(
        BankAccount[] accounts,
        int workerCount,
        int durationMs)
    {
        var totalBefore = accounts.Sum(a => a.Balance);
        var completed = 0;
        var deadlocks = 0;

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

                    if (!Monitor.TryEnter(from.LockObj, LockTimeoutMs))
                    {
                        continue;
                    }

                    try
                    {
                        if (!Monitor.TryEnter(to.LockObj, LockTimeoutMs))
                        {
                            Interlocked.Increment(ref deadlocks);
                            continue;
                        }

                        try
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
                        finally
                        {
                            Monitor.Exit(to.LockObj);
                        }
                    }
                    finally
                    {
                        Monitor.Exit(from.LockObj);
                    }
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var totalAfter = accounts.Sum(a => a.Balance);
        return (totalBefore, totalAfter, completed, deadlocks);
    }
}
