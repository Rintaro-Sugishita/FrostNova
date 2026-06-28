namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Tuning parameters for <see cref="GeneticAlgorithmStrategy"/>.
/// </summary>
public record GaParameters(
    int PopulationSize = 50,
    int Generations = 100,
    double MutationRate = 0.05,
    int? Seed = null
);

/// <summary>
/// Optimization strategy that uses a genetic algorithm with OX (Order Crossover) and 2-point swap mutation.
/// Elite preservation guarantees that the best individual always survives to the next generation.
/// Recommended when the number of products is large (roughly 10 or more) and exhaustive search is infeasible.
/// </summary>
public sealed class GeneticAlgorithmStrategy : IOptimizationStrategy
{
    private readonly GaParameters _params;

    public GeneticAlgorithmStrategy(GaParameters? parameters = null)
    {
        _params = parameters ?? new GaParameters();
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
        if (itemCount == 1)
        {
            var (v, _) = simulate([0]);
            return v;
        }

        var rng = _params.Seed.HasValue ? new Random(_params.Seed.Value) : new Random();
        int popSize = _params.PopulationSize;

        // 初期集団の生成と評価
        var population = new int[popSize][];
        var evaluated = new (ScheduleVariant Variant, ScheduleScore Score)[popSize];

        for (int i = 0; i < popSize; i++)
        {
            ct.ThrowIfCancellationRequested();
            var arr = Enumerable.Range(0, itemCount).ToArray();
            FisherYatesShuffle(arr, rng);
            population[i] = arr;
            evaluated[i] = simulate((int[])arr.Clone());
        }

        ScheduleVariant? bestVariant = null;
        double bestScore = double.MaxValue;
        int bestIdx = 0;
        UpdateBest(evaluated, popSize, ref bestVariant, ref bestScore, ref bestIdx);

        // 世代ループ
        for (int gen = 0; gen < _params.Generations; gen++)
        {
            ct.ThrowIfCancellationRequested();

            var newPop = new int[popSize][];
            var newEval = new (ScheduleVariant Variant, ScheduleScore Score)[popSize];

            // エリート保存：最良個体をそのまま次世代へ
            newPop[0] = (int[])population[bestIdx].Clone();
            newEval[0] = evaluated[bestIdx];

            for (int i = 1; i < popSize; i++)
            {
                ct.ThrowIfCancellationRequested();

                // トーナメント選択（k=3）
                var p1 = TournamentSelect(population, evaluated, rng);
                var p2 = TournamentSelect(population, evaluated, rng);

                // OX 交叉
                int cut1 = rng.Next(itemCount);
                int cut2 = rng.Next(itemCount);
                if (cut1 > cut2) (cut1, cut2) = (cut2, cut1);
                var child = OrderCrossover(p1, p2, cut1, cut2, itemCount);

                // 2 点 swap 突然変異
                if (rng.NextDouble() < _params.MutationRate)
                {
                    int a = rng.Next(itemCount);
                    int b = rng.Next(itemCount);
                    (child[a], child[b]) = (child[b], child[a]);
                }

                newPop[i] = child;
                newEval[i] = simulate((int[])child.Clone());
            }

            population = newPop;
            evaluated = newEval;
            UpdateBest(evaluated, popSize, ref bestVariant, ref bestScore, ref bestIdx);
        }

        return bestVariant!;
    }

    private static void UpdateBest(
        (ScheduleVariant Variant, ScheduleScore Score)[] evaluated,
        int count,
        ref ScheduleVariant? best,
        ref double bestScore,
        ref int bestIdx)
    {
        for (int i = 0; i < count; i++)
        {
            var s = evaluated[i].Score;
            if (!s.IsHardViolated && s.TotalScore < bestScore)
            {
                bestScore = s.TotalScore;
                best = evaluated[i].Variant;
                bestIdx = i;
            }
            best ??= evaluated[i].Variant;
        }
    }

    private static int[] TournamentSelect(
        int[][] population,
        (ScheduleVariant Variant, ScheduleScore Score)[] evaluated,
        Random rng,
        int k = 3)
    {
        int winner = rng.Next(population.Length);
        for (int i = 1; i < k; i++)
        {
            int challenger = rng.Next(population.Length);
            var ws = evaluated[winner].Score;
            var cs = evaluated[challenger].Score;
            if (!cs.IsHardViolated && (ws.IsHardViolated || cs.TotalScore < ws.TotalScore))
                winner = challenger;
        }
        return population[winner];
    }

    private static int[] OrderCrossover(int[] parent1, int[] parent2, int cut1, int cut2, int n)
    {
        var child = new int[n];
        var inSegment = new HashSet<int>();

        // parent1 の [cut1, cut2] 区間をそのまま継承
        for (int i = cut1; i <= cut2; i++)
        {
            child[i] = parent1[i];
            inSegment.Add(parent1[i]);
        }

        // 残りを parent2 の相対順序で埋める
        int pos = (cut2 + 1) % n;
        int src = (cut2 + 1) % n;
        int filled = 0;
        int remaining = n - (cut2 - cut1 + 1);

        while (filled < remaining)
        {
            if (!inSegment.Contains(parent2[src]))
            {
                child[pos] = parent2[src];
                pos = (pos + 1) % n;
                filled++;
            }
            src = (src + 1) % n;
        }

        return child;
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
