namespace FrostNova.Scheduling.Engine;

/// <summary>
/// 指定した回数だけランダムに順列をシャッフルしてシミュレーションを実行する探索戦略。
/// 製品数が多く全探索が現実的でない場合に使用する。
/// Fisher-Yates シャッフルで各試行の順列を生成し、最小スコアの結果を返す。
/// </summary>
public sealed class RandomSearchStrategy : IOptimizationStrategy
{
    private readonly int _iterations;
    private readonly int? _seed;

    /// <param name="iterations">シミュレーションを実行する回数。</param>
    /// <param name="seed">乱数シード。null の場合は非決定的。テスト再現性のために指定する。</param>
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
