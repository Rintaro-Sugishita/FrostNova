namespace FrostNova.Scheduling.Engine;

/// <summary>
/// シミュレーション結果（ScheduleVariant）を評価してスコアを返すインターフェース。
/// </summary>
public interface IScheduleEvaluator
{
    ScheduleScore Evaluate(ScheduleVariant variant);
}

/// <summary>
/// デフォルト評価実装。
/// TotalScore = A × 希望納期ズレ[h] + B × 総段取り時間[h] + C × 平均在庫日数 + D × ライン稼働バランス[h]
/// 絶対納期（Deadline）を超えた場合は ScheduleScore.HardViolated を返す。
/// </summary>
public sealed class DefaultScheduleEvaluator : IScheduleEvaluator
{
    private readonly ScoreWeights _weights;

    // (最終ステップ参照, IScheduleDeadline) — ハード違反チェック・希望納期ズレ・重要度ウェイト計算に使用
    private readonly IReadOnlyList<(object FinalStep, IScheduleDeadline Deadline)> _deadlineByFinalStep;

    // (ステップ参照, SetupTime, EarliestStart?) — 段取り時間合計と在庫ペナルティ計算に使用
    private readonly IReadOnlyList<(object Step, TimeSpan SetupTime, DateTime? EarliestStart)> _stepInfo;

    public DefaultScheduleEvaluator(
        IReadOnlyList<(object FinalStep, IScheduleDeadline Deadline)> deadlineByFinalStep,
        IReadOnlyList<(object Step, TimeSpan SetupTime, DateTime? EarliestStart)> stepInfo,
        ScoreWeights weights)
    {
        _deadlineByFinalStep = deadlineByFinalStep;
        _stepInfo = stepInfo;
        _weights = weights;
    }

    public ScheduleScore Evaluate(ScheduleVariant variant)
    {
        // 1. ハード制約チェック（絶対納期違反）
        foreach (var (finalStep, deadline) in _deadlineByFinalStep)
        {
            if (variant.Snapshots.TryGetValue(finalStep, out var snap) && snap.End.HasValue)
            {
                if (snap.End.Value > deadline.Deadline)
                    return ScheduleScore.HardViolated;
            }
        }

        // 2. 希望納期からのズレ計算（ImportanceWeight 付き）
        double targetDeviationHours = 0;
        foreach (var (finalStep, deadline) in _deadlineByFinalStep)
        {
            if (variant.Snapshots.TryGetValue(finalStep, out var snap) && snap.End.HasValue)
            {
                var deviation = snap.End.Value - deadline.TargetDate;
                if (deviation > TimeSpan.Zero)
                    targetDeviationHours += deadline.ImportanceWeight * deviation.TotalHours;
            }
        }

        // 3. 総段取り時間（Phase 2 で ISetupMatrix による動的値に対応予定）
        double totalSetupHours = _stepInfo.Sum(x => x.SetupTime.TotalHours);

        // 4. 在庫ペナルティ（EarliestStart より前倒しになった量の平均）
        double totalInventoryDays = 0;
        int inventoryCount = 0;
        foreach (var (step, _, earliestStart) in _stepInfo)
        {
            if (earliestStart.HasValue
                && variant.Snapshots.TryGetValue(step, out var snap)
                && snap.Start.HasValue)
            {
                var overrun = earliestStart.Value - snap.Start.Value;
                if (overrun > TimeSpan.Zero)
                {
                    totalInventoryDays += overrun.TotalDays;
                    inventoryCount++;
                }
            }
        }
        double avgInventoryDays = inventoryCount > 0 ? totalInventoryDays / inventoryCount : 0;

        // 5. ライン稼働バランス（各ラインの稼働時間[h]の標準偏差）
        var lineWorking = new Dictionary<object, double>(EqualityComparer<object>.Default);
        foreach (var (step, _, _) in _stepInfo)
        {
            if (variant.Snapshots.TryGetValue(step, out var snap)
                && snap.LineId != null
                && snap.WorkingSpan.HasValue)
            {
                lineWorking.TryGetValue(snap.LineId, out var current);
                lineWorking[snap.LineId] = current + snap.WorkingSpan.Value.TotalHours;
            }
        }
        double lineBalanceStdDev = 0;
        if (lineWorking.Count > 1)
        {
            var values = lineWorking.Values.ToList();
            var avg = values.Average();
            var variance = values.Average(v => (v - avg) * (v - avg));
            lineBalanceStdDev = Math.Sqrt(variance);
        }

        // 6. 加重和スコア
        var totalScore = _weights.A * targetDeviationHours
                       + _weights.B * totalSetupHours
                       + _weights.C * avgInventoryDays
                       + _weights.D * lineBalanceStdDev;

        return new ScheduleScore
        {
            IsHardViolated = false,
            TargetDateDeviationHours = targetDeviationHours,
            TotalSetupHours = totalSetupHours,
            AverageInventoryDays = avgInventoryDays,
            LineBalanceStdDevHours = lineBalanceStdDev,
            TotalScore = totalScore,
        };
    }
}
