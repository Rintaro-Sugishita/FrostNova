namespace FrostNova.Scheduling.Engine;

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
