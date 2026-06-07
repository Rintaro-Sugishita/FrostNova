namespace FrostNova.Scheduling.Engine;

public static class ScheduleEvaluator
{
    /// <summary>
    /// スケジュール結果をマルチクライテリアスコアに変換する。
    /// </summary>
    public static ScheduleScore Evaluate<TOperation, TOperationKey>(
        IEnumerable<TOperation> scheduledSteps,
        IReadOnlyList<ScheduleViolationInfo> violations,
        ScheduleScoreWeights weights)
        where TOperation : IProcessStep<TOperationKey>
    {
        var steps = scheduledSteps.ToList();

        // 納期遅れペナルティ（分）
        double deadlinePenalty = violations
            .Where(v => v.Type == ScheduleViolationType.DeadlineMiss)
            .Sum(v => (v.ActualDate - v.ConstraintDate).TotalMinutes);

        // 全工程 WorkingSpan 合計（分）
        double totalLeadTime = steps
            .Where(s => s.WorkingSpan.HasValue)
            .Sum(s => s.WorkingSpan!.Value.TotalMinutes);

        // 段取り工程数
        int setupCount = steps.Count(s => s.SetupTime > TimeSpan.Zero);

        double totalScore =
            weights.DeadlineWeight * deadlinePenalty +
            weights.LeadTimeWeight * totalLeadTime +
            weights.SetupWeight * setupCount;

        return new ScheduleScore(deadlinePenalty, totalLeadTime, setupCount, totalScore);
    }
}
