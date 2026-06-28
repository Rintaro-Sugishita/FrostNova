namespace FrostNova.Scheduling.Engine;
/*
public record ScheduleScore(
    /// <summary>納期遅れ合計（分）。小さいほど良い。</summary>
    double DeadlinePenaltyMinutes,
    /// <summary>全工程の WorkingSpan 合計（分）。小さいほど仕掛在庫が少ない。</summary>
    double TotalLeadTimeMinutes,
    /// <summary>SetupTime > 0 の工程数。小さいほど段取り替えが少ない。</summary>
    int SetupCount,
    /// <summary>重み付き合計スコア。小さいほど良い解。</summary>
    double TotalScore
);

public record ScheduleScoreWeights(
    double DeadlineWeight = 1.0,
    double LeadTimeWeight = 0.001,
    double SetupWeight = 0.01
);
*/

/// <summary>
/// Weighting coefficients for the schedule score formula:
/// <c>TotalScore = A × target-date deviation [h] + B × total setup time [h] + C × average inventory days + D × line-balance std-dev [h]</c>
/// </summary>
public record ScoreWeights(
    double A = 1.0,
    double B = 1.0,
    double C = 1.0,
    double D = 1.0
);

/// <summary>
/// Detailed score breakdown for a single simulation run. Lower <see cref="TotalScore"/> is better.
/// </summary>
public record ScheduleScore
{
    /// <summary>
    /// <see langword="true"/> when the absolute deadline (Deadline) was exceeded.
    /// In this case <see cref="TotalScore"/> is treated as <see cref="double.MaxValue"/>.
    /// </summary>
    public bool IsHardViolated { get; init; }

    /// <summary>Sum of deviations from the desired completion date (TargetDate), in hours. Multiplied by weight A.</summary>
    public double TargetDateDeviationHours { get; init; }

    /// <summary>Total setup time across all operations, in hours. Multiplied by weight B.</summary>
    public double TotalSetupHours { get; init; }

    /// <summary>
    /// Inventory penalty: average days by which operations were scheduled earlier than their EarliestStart.
    /// Multiplied by weight C.
    /// </summary>
    public double AverageInventoryDays { get; init; }

    /// <summary>
    /// Standard deviation of line utilization hours across resources. Multiplied by weight D.
    /// Smaller values indicate more even load balancing.
    /// </summary>
    public double LineBalanceStdDevHours { get; init; }

    /// <summary>Weighted composite score. <see cref="double.MaxValue"/> when <see cref="IsHardViolated"/> is <see langword="true"/>.</summary>
    public double TotalScore { get; init; }

    /// <summary>Singleton representing a hard deadline violation. Excluded from optimization by its <see cref="double.MaxValue"/> score.</summary>
    public static readonly ScheduleScore HardViolated =
        new() { IsHardViolated = true, TotalScore = double.MaxValue };
}
