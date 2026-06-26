namespace FrostNova.Scheduling.Engine;

/// <summary>IProcessStep の計算結果スナップショット。</summary>
public record StepSnapshot(
    DateTime? Start,
    DateTime? End,
    TimeSpan? WorkingSpan,
    object? LineId,
    object? MemberId
);

/// <summary>
/// 1 回のシミュレーション結果を IProcessStep への書き込みとは分離して保持するスナップショット。
/// SimulationEngine が複数回のシミュレーション結果を比較するために使用する。
/// </summary>
public class ScheduleVariant
{
    internal Dictionary<object, StepSnapshot> Snapshots { get; }

    /// <summary>このバリアントが使用した品目処理順のインデックス順列（デバッグ・再現用）。</summary>
    public IReadOnlyList<int>? OrderPermutation { get; init; }

    /// <summary>評価スコア。SimulationEngine が Evaluate() 呼び出し後に設定する。</summary>
    public ScheduleScore? Score { get; set; }

    internal ScheduleVariant(Dictionary<object, StepSnapshot> snapshots)
    {
        Snapshots = snapshots;
    }

    /// <summary>
    /// このバリアントの計算結果を IProcessStep に書き戻す。
    /// 最良解が確定したときにのみ呼ぶこと。
    /// </summary>
    public void Apply<TOperation, TKey>(IEnumerable<TOperation> steps)
        where TOperation : IProcessStep<TKey>
    {
        foreach (var step in steps)
        {
            if (Snapshots.TryGetValue(step, out var snap))
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
