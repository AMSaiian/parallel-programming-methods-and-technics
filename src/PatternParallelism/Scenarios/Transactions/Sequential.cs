namespace PatternParallelism.Scenarios.Transactions;

internal static class Sequential
{
    internal static AggregationResult Run(string filePath)
    {
        decimal totalBefore = 0m;
        decimal totalAfter = 0m;
        long premiumCount = 0;
        long totalCount = 0;

        foreach (var tx in Common.ReadTransactions(filePath))
        {
            var amountUsd = Common.ConvertToUsd(tx.Amount, tx.Currency);
            totalBefore += amountUsd;
            totalAfter += Common.ApplyCashback(amountUsd, tx.UserId);
            totalCount++;
            if (Common.IsPremium(tx.UserId))
            {
                premiumCount++;
            }
        }

        return new AggregationResult(totalBefore, totalAfter, premiumCount, totalCount);
    }
}
