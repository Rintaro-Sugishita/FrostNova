namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Optimization loop that calls <see cref="BomScheduler.Schedule"/> repeatedly with different node orderings
/// and returns the run with the best score.
/// </summary>
public static class BomScheduleOptimizer
{
    /// <summary>
    /// Schedules nodes in multiple orderings and returns the node collection whose result has the lowest score.
    /// </summary>
    /// <typeparam name="TBom">BOM node type.</typeparam>
    /// <typeparam name="TBomKey">BOM node key type.</typeparam>
    /// <typeparam name="TOperation">Process step type.</typeparam>
    /// <typeparam name="TOperationKey">Process step key type.</typeparam>
    /// <param name="targetNodes">Nodes to schedule.</param>
    /// <param name="referenceNodes">Already-confirmed reservation nodes.</param>
    /// <param name="nonWorkingTimes">Global non-working time ranges.</param>
    /// <param name="resourceNonWorkingGetter">Returns resource-specific non-working time ranges.</param>
    /// <param name="baseDateGetter">Returns base-date information for each operation.</param>
    /// <param name="isForward"><see langword="true"/> for forward scheduling; <see langword="false"/> for backward.</param>
    /// <param name="unit">Minimum scheduling time unit.</param>
    /// <param name="nodeCloner">
    /// Factory that deep-copies the node graph before each iteration.
    /// The returned collection must have the same order and structure as the input but with
    /// Start, End, LineId, and MemberId reset to <see langword="null"/>.
    /// </param>
    /// <param name="weights">Weighting coefficients for the score formula.</param>
    /// <param name="strategy">Search strategy; defaults to <see cref="HillClimbingStrategy{TBom}"/> when <see langword="null"/>.</param>
    /// <param name="iterations">Maximum number of iterations.</param>
    /// <param name="comparers">Equality comparers keyed by resource type.</param>
    /// <returns>A tuple of the best node collection, its score, and any constraint violations.</returns>
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
