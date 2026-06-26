namespace FrostNova.Scheduling.Engine;

/// <summary>
/// 最適化探索戦略のインターフェース。
/// SimulationEngine が simulate デリゲートを渡し、戦略側が反復実行して最良の ScheduleVariant を返す。
/// </summary>
public interface IOptimizationStrategy
{
    /// <summary>
    /// 最適化を実行し、最良スコアの ScheduleVariant を返す。
    /// </summary>
    /// <param name="itemCount">処理順を最適化する品目数（順列の次元）。0 の場合は空の順列で 1 回だけ simulate を呼ぶ。</param>
    /// <param name="simulate">
    /// SimulationEngine が提供するシミュレーション関数。
    /// 引数: int[]（品目インデックスの順列）→ (ScheduleVariant, ScheduleScore)
    /// ハード違反も除外せずに返す（Score.IsHardViolated で判断すること）。
    /// </param>
    /// <param name="ct">中断トークン。各反復の前に確認すること。</param>
    /// <returns>最良スコアの ScheduleVariant。ハード違反以外の有効な解が1つもない場合は最後に評価した結果を返す。</returns>
    ScheduleVariant Optimize(
        int itemCount,
        Func<int[], (ScheduleVariant Variant, ScheduleScore Score)> simulate,
        CancellationToken ct = default);
}
