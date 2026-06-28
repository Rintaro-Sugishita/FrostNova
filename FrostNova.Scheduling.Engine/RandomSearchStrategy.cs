namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Optimization strategy that shuffles the item permutation randomly for a fixed number of iterations
/// and returns the result with the lowest score. Uses Fisher-Yates shuffle for each trial.
/// Suitable when the product count is too large for exhaustive search.
/// </summary>
public sealed class RandomSearchStrategy : IOptimizationStrategy
{
    private readonly int _iterations;
    private readonly int? _seed;

    /// <param name="iterations">Number of simulation runs to execute.</param>
    /// <param name="seed">Random seed. Pass <see langword="null"/> for non-deterministic behavior; specify a value for test reproducibility.</param>
    public RandomSearchStrategy(int iterations, int? seed = null)
    {
        _iterations = iterations > 0 ? iterations : throw new ArgumentOutOfRangeException(nameof(iterations), "1 以上を指定してください。");
        _seed = seed;
    }

    public ScheduleVariant Optimize(
        int itemCount,
        Func<int[], (ScheduleVariant Variant, ScheduleScore Score)> simulate,
        CancellationToken ct = default)
    {
        if (itemCount <= 0)
        {
            var (v, _) = simulate([]);
            return v;
        }

        var rng = _seed.HasValue ? new Random(_seed.Value) : new Random();
        ScheduleVariant? best = null;
        double bestScore = double.MaxValue;

        var arr = Enumerable.Range(0, itemCount).ToArray();

        for (int i = 0; i < _iterations; i++)
        {
            ct.ThrowIfCancellationRequested();

            FisherYatesShuffle(arr, rng);
            var perm = (int[])arr.Clone();
            var (variant, score) = simulate(perm);

            if (!score.IsHardViolated && score.TotalScore < bestScore)
            {
                bestScore = score.TotalScore;
                best = variant;
            }
            best ??= variant;
        }

        return best!;
    }

    private static void FisherYatesShuffle(int[] arr, Random rng)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
