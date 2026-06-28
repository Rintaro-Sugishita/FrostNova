namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Search strategy for schedule optimization. Generates candidate orderings (different priority assignments)
/// for the given node list so the optimizer can evaluate each one.
/// </summary>
public interface IScheduleOptimizationStrategy<TBom>
{
    IEnumerable<IReadOnlyList<TBom>> GenerateCandidateOrderings(IReadOnlyList<TBom> nodes, int maxIterations);
}
