namespace MultiProcessAndResourceCompetitiveParallelism.Scenarios.BankTransfer;

internal class BankAccount
{
    public BankAccount(int id, decimal balance)
    {
        Id = id;
        Balance = balance;
    }

    public int Id { get; }
    public decimal Balance { get; set; }
    public object LockObj { get; } = new();
}

internal static class Common
{
    internal static BankAccount[] CreateAccounts(int count, int seed)
    {
        var rng = new Random(seed);
        return Enumerable.Range(0, count)
                         .Select(i => new BankAccount(i, Math.Round((decimal)(rng.NextDouble() * 9_900 + 100), 2)))
                         .ToArray();
    }
}
