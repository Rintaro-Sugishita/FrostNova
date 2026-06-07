namespace FrostNova.Scheduling.Engine;

/// <summary>
/// スケジュール最適化における探索戦略のインターフェース。
/// 与えられたノードリストに対して、異なる優先順位付け（候補順序）を生成する。
/// </summary>
public interface IScheduleOptimizationStrategy<TBom>
{
    IEnumerable<IReadOnlyList<TBom>> GenerateCandidateOrderings(IReadOnlyList<TBom> nodes, int maxIterations);
}
