namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Pluggable ordering optimization strategy. The simulation engine provides a <c>simulate</c> delegate and
/// the strategy calls it repeatedly to find the best <see cref="ScheduleVariant"/>.
/// </summary>
public interface IOptimizationStrategy
{
    /// <summary>
    /// Runs the optimization and returns the variant with the best score.
    /// </summary>
    /// <param name="itemCount">
    /// Number of items whose processing order is being optimized (the permutation dimension).
    /// Pass 0 to invoke <paramref name="simulate"/> once with an empty permutation.
    /// </param>
    /// <param name="simulate">
    /// Simulation function supplied by the engine.
    /// Takes an <c>int[]</c> permutation of item indices and returns a <c>(ScheduleVariant, ScheduleScore)</c> pair.
    /// Hard-violated results are included in the output; check <see cref="ScheduleScore.IsHardViolated"/> before using.
    /// </param>
    /// <param name="ct">Cancellation token; implementations must check it before each iteration.</param>
    /// <returns>
    /// The variant with the lowest score. If all evaluated results are hard-violated, returns the last
    /// evaluated result rather than throwing.
    /// </returns>
    ScheduleVariant Optimize(
        int itemCount,
        Func<int[], (ScheduleVariant Variant, ScheduleScore Score)> simulate,
        CancellationToken ct = default);
}
