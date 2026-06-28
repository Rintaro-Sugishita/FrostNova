using static FrostNova.Scheduling.Engine.BomScheduler;

namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Drives repeated calls to <see cref="BomScheduler"/>, evaluates each result with an
/// <see cref="IScheduleEvaluator"/>, and returns the best <see cref="ScheduleVariant"/> found by the
/// chosen <see cref="IOptimizationStrategy"/>. Decouples direct writes to <c>IProcessStep</c> from
/// simulation results by running a Backup → ApplyPriority → Schedule → Snapshot → Restore cycle.
/// </summary>
public static class SimulationEngine
{
    private sealed record EngineBackup(
        Dictionary<object, int> NodePriorities,
        Dictionary<object, StepSnapshot> StepSnapshots
    );

    // ────────────────────────────────────────────────────────────────
    // Public エントリポイント — ScoreWeights を渡すと DefaultScheduleEvaluator を自動構築する
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a <see cref="DefaultScheduleEvaluator"/> from the supplied <see cref="ScoreWeights"/> and
    /// runs the optimization. Target-date deviation and inventory penalty are computed only when BOM nodes
    /// also implement <see cref="IScheduleDeadline"/>.
    /// </summary>
    public static ScheduleVariant Optimize<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> targetNodes,
        IEnumerable<TBom> referenceNodes,
        IEnumerable<IPendingTimeRange> nonWorkingTimes,
        Func<ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
        Func<TOperation, BaseDateInfo> baseDateGetter,
        bool isForward,
        TimeSpan unit,
        ScoreWeights weights,
        IOptimizationStrategy strategy,
        ISetupMatrix? setupMatrix = null,
        Dictionary<ResourceType, IEqualityComparer<object>>? comparers = null,
        CancellationToken ct = default)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var evaluator = BuildDefaultEvaluator<TBom, TBomKey, TOperation, TOperationKey>(targetNodes, weights);
        return Optimize<TBom, TBomKey, TOperation, TOperationKey>(
            targetNodes, referenceNodes, nonWorkingTimes, resourceNonWorkingGetter,
            baseDateGetter, isForward, unit, evaluator, strategy, setupMatrix, comparers, ct);
    }

    // ────────────────────────────────────────────────────────────────
    // Public エントリポイント — カスタム IScheduleEvaluator を渡す（本体）
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// カスタム評価器を使って最適化を実行する。
    /// 最良解が確定したら IProcessStep に書き戻し、ScheduleVariant を返す。
    /// </summary>
    public static ScheduleVariant Optimize<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> targetNodes,
        IEnumerable<TBom> referenceNodes,
        IEnumerable<IPendingTimeRange> nonWorkingTimes,
        Func<ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
        Func<TOperation, BaseDateInfo> baseDateGetter,
        bool isForward,
        TimeSpan unit,
        IScheduleEvaluator evaluator,
        IOptimizationStrategy strategy,
        ISetupMatrix? setupMatrix = null,
        Dictionary<ResourceType, IEqualityComparer<object>>? comparers = null,
        CancellationToken ct = default)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var backup = CreateBackup<TBom, TBomKey, TOperation, TOperationKey>(targetNodes);

        // IScheduleDeadline.RiskBuffer が設定されているノードのステップに LeadTime 係数を適用する関数
        var nodeByStep = BuildNodeByStepMap<TBom, TBomKey, TOperation, TOperationKey>(targetNodes);
        Func<TOperation, TimeSpan>? leadTimeOverride = BuildLeadTimeOverride<TBom, TBomKey, TOperation, TOperationKey>(
            nodeByStep, unit);

        (ScheduleVariant, ScheduleScore) Simulate(int[] order)
        {
            // 前回の状態にリセット
            ApplyBackup<TBom, TBomKey, TOperation, TOperationKey>(backup, targetNodes);

            // 品目処理順を Priority に反映（BomScheduler 内部の Priority ソートに乗せる）
            ApplyPriority<TBom, TBomKey, TOperation, TOperationKey>(targetNodes, order);

            Schedule<TBom, TBomKey, TOperation, TOperationKey>(
                targetNodes, referenceNodes, nonWorkingTimes,
                resourceNonWorkingGetter, baseDateGetter, isForward, unit, leadTimeOverride, setupMatrix, comparers);

            var variant = TakeSnapshot<TBom, TBomKey, TOperation, TOperationKey>(targetNodes, order);
            var score = evaluator.Evaluate(variant);
            variant.Score = score;
            return (variant, score);
        }

        var best = strategy.Optimize(targetNodes.Count, Simulate, ct);

        // 最良解を IProcessStep に書き戻す
        ApplyBackup<TBom, TBomKey, TOperation, TOperationKey>(backup, targetNodes);
        best.Apply<TOperation, TOperationKey>(targetNodes.SelectMany(n => n.ProcessSteps));

        // IBomNode.Priority を元の値に戻す
        RestorePriority<TBom, TBomKey, TOperation, TOperationKey>(backup, targetNodes);

        return best;
    }

    // ────────────────────────────────────────────────────────────────
    // Private: Backup / Restore
    // ────────────────────────────────────────────────────────────────

    private static EngineBackup CreateBackup<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> nodes)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var priorities = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);
        var steps = new Dictionary<object, StepSnapshot>(ReferenceEqualityComparer.Instance);

        foreach (var node in nodes)
        {
            priorities[node] = node.Priority;
            foreach (var step in node.ProcessSteps)
                steps[step] = new StepSnapshot(step.Start, step.End, step.WorkingSpan, step.LineId, step.MemberId);
        }

        return new EngineBackup(priorities, steps);
    }

    private static void ApplyBackup<TBom, TBomKey, TOperation, TOperationKey>(
        EngineBackup backup, IReadOnlyList<TBom> nodes)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        foreach (var node in nodes)
        {
            if (backup.NodePriorities.TryGetValue(node, out var p)) node.Priority = p;
            foreach (var step in node.ProcessSteps)
            {
                if (backup.StepSnapshots.TryGetValue(step, out var snap))
                {
                    step.Start = snap.Start;
                    step.End = snap.End;
                    step.WorkingSpan = snap.WorkingSpan;
                    step.LineId = snap.LineId;
                    step.MemberId = snap.MemberId;
                }
            }
        }
    }

    private static void RestorePriority<TBom, TBomKey, TOperation, TOperationKey>(
        EngineBackup backup, IReadOnlyList<TBom> nodes)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        foreach (var node in nodes)
        {
            if (backup.NodePriorities.TryGetValue(node, out var p))
                node.Priority = p;
        }
    }

    // ────────────────────────────────────────────────────────────────
    // Private: Priority 操作（製品処理順の反映）
    // ────────────────────────────────────────────────────────────────

    private static void ApplyPriority<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> nodes, int[] order)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        // order が空（itemCount == 0）の場合は何もしない
        if (order.Length == 0) return;

        // nodes[order[i]].Priority = i として BomScheduler のソート順を制御する
        for (int i = 0; i < order.Length; i++)
            nodes[order[i]].Priority = i;
    }

    // ────────────────────────────────────────────────────────────────
    // Private: Snapshot
    // ────────────────────────────────────────────────────────────────

    private static ScheduleVariant TakeSnapshot<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> nodes, int[] order)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var dict = new Dictionary<object, StepSnapshot>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
            foreach (var step in node.ProcessSteps)
                dict[step] = new StepSnapshot(step.Start, step.End, step.WorkingSpan, step.LineId, step.MemberId);

        return new ScheduleVariant(dict)
        {
            OrderPermutation = order.Length > 0 ? order.ToList() : null
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Private: RiskBuffer — LeadTime 係数適用
    // ────────────────────────────────────────────────────────────────

    private static Dictionary<object, TBom> BuildNodeByStepMap<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> nodes)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var map = new Dictionary<object, TBom>(ReferenceEqualityComparer.Instance);
        foreach (var node in nodes)
            foreach (var step in node.ProcessSteps)
                map[step] = node;
        return map;
    }

    private static Func<TOperation, TimeSpan>? BuildLeadTimeOverride<TBom, TBomKey, TOperation, TOperationKey>(
        Dictionary<object, TBom> nodeByStep,
        TimeSpan unit)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        // RiskBuffer を持つノードが1つもなければ null を返す（BomScheduler の既存パスを使う）
        if (!nodeByStep.Values.Any(n => n is IScheduleDeadline { RiskBuffer: > 0 }))
            return null;

        return step =>
        {
            var baseTime = step.LeadTime ?? unit;
            if (nodeByStep.TryGetValue(step, out var node)
                && node is IScheduleDeadline dl
                && dl.RiskBuffer > 0)
            {
                return baseTime * (1.0 + dl.RiskBuffer);
            }
            return baseTime;
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Private: DefaultScheduleEvaluator の構築
    // ────────────────────────────────────────────────────────────────

    private static DefaultScheduleEvaluator BuildDefaultEvaluator<TBom, TBomKey, TOperation, TOperationKey>(
        IReadOnlyList<TBom> targetNodes, ScoreWeights weights)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var deadlineByFinalStep = new List<(object, IScheduleDeadline)>();
        var stepInfo = new List<(object Step, TimeSpan SetupTime, DateTime? EarliestStart)>();

        foreach (var node in targetNodes)
        {
            // 最終工程（最大 StepOrder のステップ）が納期チェックの対象
            var finalStep = node.ProcessSteps
                .OrderByDescending(s => s.StepOrder)
                .FirstOrDefault();

            if (finalStep != null && node is IScheduleDeadline deadline)
                deadlineByFinalStep.Add((finalStep, deadline));

            // 在庫許容限界は同一ノードの全ステップに適用
            DateTime? earliestStart = (node is IScheduleDeadline dl) ? dl.EarliestStart : null;

            foreach (var step in node.ProcessSteps)
                stepInfo.Add((step, step.SetupTime, earliestStart));
        }

        return new DefaultScheduleEvaluator(deadlineByFinalStep, stepInfo, weights);
    }
}
