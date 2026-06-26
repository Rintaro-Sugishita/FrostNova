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
/// スコア計算式の重みパラメータ。
/// TotalScore = A × 希望納期ズレ[h] + B × 総段取り時間[h] + C × 平均在庫日数 + D × ライン稼働バランス[h]
/// </summary>
public record ScoreWeights(
    double A = 1.0,
    double B = 1.0,
    double C = 1.0,
    double D = 1.0
);

/// <summary>
/// 1 回のシミュレーション結果のスコア内訳。TotalScore が小さいほど良い解。
/// </summary>
public record ScheduleScore
{
    /// <summary>絶対納期（Deadline）を超えたシナリオは true。この場合 TotalScore は double.MaxValue 扱い。</summary>
    public bool IsHardViolated { get; init; }

    /// <summary>希望納期（TargetDate）からのズレ時間の合計（時間）。A × この値がスコアに加算される。</summary>
    public double TargetDateDeviationHours { get; init; }

    /// <summary>総段取り時間（時間）。B × この値がスコアに加算される。</summary>
    public double TotalSetupHours { get; init; }

    /// <summary>在庫ペナルティ：EarliestStart より前倒しになった工程のズレ平均（日）。C × この値がスコアに加算される。</summary>
    public double AverageInventoryDays { get; init; }

    /// <summary>ライン稼働時間の標準偏差（時間）。D × この値がスコアに加算される。値が小さいほど均等に稼働している。</summary>
    public double LineBalanceStdDevHours { get; init; }

    /// <summary>加重和スコア。IsHardViolated == true の場合は double.MaxValue。小さいほど良い。</summary>
    public double TotalScore { get; init; }

    /// <summary>絶対納期違反のシングルトン。TotalScore = double.MaxValue として評価から除外される。</summary>
    public static readonly ScheduleScore HardViolated =
        new() { IsHardViolated = true, TotalScore = double.MaxValue };
}
