namespace FrostNova.Scheduling.Engine;

public static class ScheduleEvaluator
{
    /// <summary>
    /// Converts a schedule result into a multi-criteria <see cref="ScheduleScore"/>.
    /// </summary>
    public static ScheduleScore Evaluate<TOperation, TOperationKey>(
        IEnumerable<TOperation> scheduledSteps,
        IReadOnlyList<ScheduleViolationInfo> violations,
        ScoreWeights weights)
        where TOperation : IProcessStep<TOperationKey>
    {
        var steps = scheduledSteps.ToList();

        // 納期遅れ（時間）
        double targetDateDeviationHours = violations
            .Where(v => v.Type == ScheduleViolationType.DeadlineMiss)
            .Sum(v => (v.ActualDate - v.ConstraintDate).TotalHours);

        // 総段取り時間（時間）
        double totalSetupHours = steps
            .Where(s => s.SetupTime > TimeSpan.Zero)
            .Sum(s => s.SetupTime.TotalHours);

        double totalScore =
            weights.A * targetDateDeviationHours +
            weights.B * totalSetupHours;

        return new ScheduleScore
        {
            IsHardViolated = false,
            TargetDateDeviationHours = targetDateDeviationHours,
            TotalSetupHours = totalSetupHours,
            TotalScore = totalScore
        };
    }
}
