using System.CommandLine;

namespace Runner.Scenarios;

public class MatrixTransposeRunner : BaseScenario
{
    public static readonly Option<int> SizeOption = new("--dim", "-d")
    {
        Description = "Matrix dimension N (creates an N x N matrix)",
        DefaultValueFactory = _ => 10000,
        Validators =
        {
            optionResult =>
            {
                var value = optionResult.GetValueOrDefault<int>();
                if (value < 2)
                {
                    optionResult.AddError("Size must be at least 2.");
                }
            }
        }
    };

    public MatrixTransposeRunner() : base("matrix", "Transpose an N x N matrix")
    {
        Options.Add(SizeOption);
        SetAction(RunAsync);
    }

    protected override async Task RunAsync(ParseResult parseResult)
    {
        await base.RunAsync(parseResult);

        var threads = parseResult.GetValue(GlobalOptions.ThreadsOption);
        var size = parseResult.GetValue(SizeOption);
        var verbose = parseResult.GetValue(GlobalOptions.VerboseOption);

        if (verbose)
        {
            Console.WriteLine($"  matrix size: {size} x {size}");
        }

        var seed = parseResult.GetValue(GlobalOptions.SeedOption);
        var source = CreateMatrix(size);

        var (seqMs, seqResult) = await ExecuteWithTimingAsync(() => Task.FromResult(TransposeSequential(source, size)));
        Console.WriteLine($"  sequential: took {seqMs} ms");
        if (verbose)
        {
            var rng = new Random(seed);
            var i = rng.Next(0, size);
            var j = rng.Next(0, size);
            Console.WriteLine($"    verify: source[{i},{j}] = {source[i, j]}, result[{j},{i}] = {seqResult[j, i]}");
        }

        var (parMs, parResult) = await ExecuteWithTimingAsync(() => TransposeParallel(source, size, threads));
        Console.WriteLine($"  parallel:   took {parMs} ms");
        if (verbose)
        {
            var rng = new Random(seed);
            var i = rng.Next(0, size);
            var j = rng.Next(0, size);
            Console.WriteLine($"    verify: source[{i},{j}] = {source[i, j]}, result[{j},{i}] = {parResult[j, i]}");
        }
    }

    private static double[,] CreateMatrix(int size)
    {
        var matrix = new double[size, size];

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                matrix[row, col] = row * size + col;
            }
        }

        return matrix;
    }

    private static double[,] TransposeSequential(double[,] source, int size)
    {
        var result = new double[size, size];

        for (var row = 0; row < size; row++)
        {
            for (var col = 0; col < size; col++)
            {
                result[col, row] = source[row, col];
            }
        }

        return result;
    }

    private static async Task<double[,]> TransposeParallel(double[,] source, int size, int threads)
    {
        var result = new double[size, size];
        var rowsPerThread = size / threads;

        var tasks = Enumerable.Range(0, threads).Select(t => Task.Run(() =>
        {
            var startRow = t * rowsPerThread;
            int endRow;
            if (t == threads - 1)
            {
                endRow = size;
            }
            else
            {
                endRow = startRow + rowsPerThread;
            }

            for (var row = startRow; row < endRow; row++)
            {
                for (var col = 0; col < size; col++)
                {
                    result[col, row] = source[row, col];
                }
            }
        }));

        await Task.WhenAll(tasks);

        return result;
    }
}
