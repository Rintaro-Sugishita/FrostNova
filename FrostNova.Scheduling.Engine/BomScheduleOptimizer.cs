namespace FrostNova.Scheduling.Engine;

/// <summary>
/// BomScheduler.Schedule を複数回実行し、最もスコアの良いスケジュール結果を探索する最適化ループ。
/// </summary>
public static class BomScheduleOptimizer
{
    /// <summary>
    /// ノードを複数順序でスケジュールし、最良のスコアを持つ結果のノードコレクションを返す。
    /// </summary>
    /// <typeparam name="TBom">BOM ノード型</typeparam>
    /// <typeparam name="TBomKey">BOM ノードキー型</typeparam>
    /// <typeparam name="TOperation">工程ステップ型</typeparam>
    /// <typeparam name="TOperationKey">工程ステップキー型</typeparam>
    /// <param name="targetNodes">スケジュール対象ノード</param>
    /// <param name="referenceNodes">既存予約ノード</param>
    /// <param name="nonWorkingTimes">全体の非稼働時間</param>
    /// <param name="resourceNonWorkingGetter">リソース固有の非稼働時間取得関数</param>
    /// <param name="baseDateGetter">各工程の基準日情報取得関数</param>
    /// <param name="isForward">true: 順算、false: 逆算</param>
    /// <param name="unit">最小時間単位</param>
    /// <param name="nodeCloner">
    /// ノードグラフの複製ファクトリ。各イテレーション前に呼び出してクリーンな状態を得る。
    /// 返すコレクションは元のノードと同じ順序・構造を持ちつつ、Start/End/LineId/MemberId がリセットされていること。
    /// </param>
    /// <param name="weights">スコア計算の重み</param>
    /// <param name="strategy">探索戦略（null の場合は HillClimbing を使用）</param>
    /// <param name="iterations">最大イテレーション数</param>
    /// <param name="comparers">リソースキーの比較器</param>
    /// <returns>最良スケジュール結果（ノードコレクション、スコア、違反情報）</returns>
    public static (IReadOnlyList<TBom> BestNodes, ScheduleScore BestScore, IReadOnlyList<ScheduleViolationInfo> BestViolations)
        Optimize<TBom, TBomKey, TOperation, TOperationKey>(
            IReadOnlyList<TBom> targetNodes,
            IEnumerable<TBom> referenceNodes,
            IEnumerable<IPendingTimeRange> nonWorkingTimes,
            Func<BomScheduler.ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
            Func<TOperation, BaseDateInfo> baseDateGetter,
            bool isForward,
            TimeSpan unit,
            Func<IReadOnlyList<TBom>, IReadOnlyList<TBom>> nodeCloner,
            ScoreWeights? weights = null,
            IScheduleOptimizationStrategy<TBom>? strategy = null,
            int iterations = 50,
            Dictionary<BomScheduler.ResourceType, IEqualityComparer<object>>? comparers = null)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        weights ??= new ScoreWeights();

        if (strategy == null)
        {
            strategy = new HillClimbingStrategy<TBom>(
                getPriority: n => n.Priority,
                setPriority: (n, p) => n.Priority = p
            );
        }

        IReadOnlyList<TBom>? bestNodes = null;
        ScheduleScore? bestScore = null;
        IReadOnlyList<ScheduleViolationInfo> bestViolations = Array.Empty<ScheduleViolationInfo>();

        var refList = referenceNodes.ToList();
        var nonWorkingList = nonWorkingTimes.ToList();

        foreach (var candidateOrder in strategy.GenerateCandidateOrderings(targetNodes, iterations))
        {
            // クリーンなクローンでスケジュール実行
            var cloned = nodeCloner(candidateOrder);

            var violations = BomScheduler.Schedule<TBom, TBomKey, TOperation, TOperationKey>(
                cloned,
                refList,
                nonWorkingList,
                resourceNonWorkingGetter,
                baseDateGetter,
                isForward,
                unit,
                comparers);

            // スコア評価
            var allSteps = cloned.SelectMany(n => n.ProcessSteps);
            var score = ScheduleEvaluator.Evaluate<TOperation, TOperationKey>(allSteps, violations, weights);

            if (bestScore == null || score.TotalScore < bestScore.TotalScore)
            {
                bestScore = score;
                bestNodes = cloned;
                bestViolations = violations;
            }
        }

        return (bestNodes ?? nodeCloner(targetNodes), bestScore ?? new ScheduleScore(), bestViolations);
    }
}
