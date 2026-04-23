using System.Globalization;

namespace PatternParallelism.Scenarios.Transactions;

internal enum Currency { USD, EUR, GBP, UAH }

internal enum ProductType { Electronics, Clothing, Food, Software, Services }

internal record Transaction(int UserId,
                            decimal Amount,
                            Currency Currency,
                            DateOnly Date,
                            ProductType ProductType);

internal record struct ConvertedTransaction(int UserId, decimal AmountUsd);

internal record struct ProcessedTransaction(decimal AmountUsd, decimal FinalAmount, bool IsPremium);

internal record AggregationResult(decimal TotalBeforeCashback, decimal TotalAmountUsd, long PremiumCount, long TotalCount)
{
    internal decimal TotalCashback => TotalBeforeCashback - TotalAmountUsd;
};

internal static class Common
{
    // Users with ID above this threshold are premium → 20% cashback
    internal const int PremiumUserIdThreshold = 5_000;

    internal static readonly IReadOnlyDictionary<Currency, decimal> ToUsdRate =
        new Dictionary<Currency, decimal>
        {
            [Currency.USD] = 1.00m,
            [Currency.EUR] = 1.08m,
            [Currency.GBP] = 1.27m,
            [Currency.UAH] = 0.024m,
        };

    internal static string GetFilePath(int count, int seed) => $"../Static/transactions/{count}_Transactions_{seed}.csv";

    internal static void SeedToFile(int count, int seed)
    {
        var path = GetFilePath(count, seed);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var rng = new Random(seed);
        var currencies = Enum.GetValues<Currency>();
        var productTypes = Enum.GetValues<ProductType>();
        var baseDate = new DateOnly(2024, 1, 1);

        using var writer = new StreamWriter(path);
        writer.WriteLine("UserId,Amount,Currency,Date,ProductType");
        for (var i = 0; i < count; i++)
        {
            var userId = rng.Next(1, 10_001);
            var amount = Math.Round((decimal)(rng.NextDouble() * 999.99 + 0.01), 2);
            var currency = currencies[rng.Next(currencies.Length)];
            var date = baseDate.AddDays(rng.Next(365));
            var productType = productTypes[rng.Next(productTypes.Length)];
            writer.WriteLine($"{userId},{amount.ToString(CultureInfo.InvariantCulture)},{currency},{date:yyyy-MM-dd},{productType}");
        }
    }

    internal static IEnumerable<Transaction> ReadTransactions(string filePath)
        => File.ReadLines(filePath).Skip(1).Select(ParseLine);

    internal static Transaction ParseLine(string line)
    {
        var p = line.Split(',');
        return new Transaction(
            UserId: int.Parse(p[0]),
            Amount: decimal.Parse(p[1], CultureInfo.InvariantCulture),
            Currency: Enum.Parse<Currency>(p[2]),
            Date: DateOnly.Parse(p[3], CultureInfo.InvariantCulture),
            ProductType: Enum.Parse<ProductType>(p[4])
        );
    }

    internal static decimal ConvertToUsd(decimal amount, Currency currency)
        => amount * ToUsdRate[currency];

    internal static bool IsPremium(int userId)
        => userId > PremiumUserIdThreshold;

    internal static decimal ApplyCashback(decimal amountUsd, int userId)
        => IsPremium(userId) ? amountUsd * 0.8m : amountUsd;
}
