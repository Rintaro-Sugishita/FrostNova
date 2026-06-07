namespace FrostNova.Scheduling.Engine;

/// <summary>
/// 山登り法による最適化戦略。
/// Priority の getter/setter を受け取り、ランダムに2ノードの優先度をスワップして近傍解を生成する。
/// </summary>
public class HillClimbingStrategy<TBom> : IScheduleOptimizationStrategy<TBom>
{
    private readonly Func<TBom, int> _getPriority;
    private readonly Action<TBom, int> _setPriority;
    private readonly Random _rng;

    public HillClimbingStrategy(
        Func<TBom, int> getPriority,
        Action<TBom, int> setPriority,
        int? seed = null)
    {
        _getPriority = getPriority;
        _setPriority = setPriority;
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
    }

    public IEnumerable<IReadOnlyList<TBom>> GenerateCandidateOrderings(IReadOnlyList<TBom> nodes, int maxIterations)
    {
        // 初期順序を最初の候補として返す
        yield return nodes;

        for (int i = 0; i < maxIterations; i++)
        {
            if (nodes.Count < 2) yield break;

            var idx1 = _rng.Next(nodes.Count);
            var idx2 = _rng.Next(nodes.Count);
            while (idx2 == idx1) idx2 = _rng.Next(nodes.Count);

            // Priority 値をスワップ
            var p1 = _getPriority(nodes[idx1]);
            var p2 = _getPriority(nodes[idx2]);
            _setPriority(nodes[idx1], p2);
            _setPriority(nodes[idx2], p1);

            yield return nodes;

            // 呼び出し元がスワップを破棄（スコアが悪化）した場合に元に戻せるよう、
            // 実際のロールバックは BomScheduleOptimizer 側で行う。
            // ここではスワップ後の状態のまま次のイテレーションへ進む（山登り法）。
        }
    }
}
