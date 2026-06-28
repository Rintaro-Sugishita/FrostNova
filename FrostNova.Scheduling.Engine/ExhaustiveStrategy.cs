namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Exhaustive search strategy. Generates all permutations using Heap's Algorithm and returns the one
/// with the minimum score. Recommended for N ≤ 8 items (9! = 362,880 calls; 10! = 3,628,800 calls).
/// </summary>
public sealed class ExhaustiveStrategy : IOptimizationStrategy
{
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

        ScheduleVariant? best = null;
        double bestScore = double.MaxValue;

        var arr = Enumerable.Range(0, itemCount).ToArray();
        Generate(itemCount, arr, simulate, ref best, ref bestScore, ct);

        return best!;
    }

    private static void Generate(
        int n,
        int[] arr,
        Func<int[], (ScheduleVariant, ScheduleScore)> simulate,
        ref ScheduleVariant? best,
        ref double bestScore,
        CancellationToken ct)
    {
        if (n == 1)
        {
            ct.ThrowIfCancellationRequested();
            var (variant, score) = simulate((int[])arr.Clone());
            if (!score.IsHardViolated && score.TotalScore < bestScore)
            {
                bestScore = score.TotalScore;
                best = variant;
            }
            // ハード違反しかない場合に備えて、最初の結果だけは保持する
            best ??= variant;
            return;
        }

        for (int i = 0; i < n; i++)
        {
            ct.ThrowIfCancellationRequested();
            Generate(n - 1, arr, simulate, ref best, ref bestScore, ct);

            if (n % 2 == 0)
                (arr[i], arr[n - 1]) = (arr[n - 1], arr[i]);
            else
                (arr[0], arr[n - 1]) = (arr[n - 1], arr[0]);
        }
    }
}
