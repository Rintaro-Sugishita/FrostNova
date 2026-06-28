namespace FrostNova.Scheduling.Engine;

/// <summary>Immutable snapshot of the computed schedule for a single <c>IProcessStep</c>.</summary>
public record StepSnapshot(
    DateTime? Start,
    DateTime? End,
    TimeSpan? WorkingSpan,
    object? LineId,
    object? MemberId
);

/// <summary>
/// Holds the results of one simulation run as an in-memory snapshot, decoupled from the
/// <c>IProcessStep</c> objects. The simulation engine uses multiple variants to compare orderings
/// and only writes the best result back via <see cref="Apply{TOperation, TKey}"/>.
/// </summary>
public class ScheduleVariant
{
    internal Dictionary<object, StepSnapshot> Snapshots { get; }

    /// <summary>The index permutation used to order items in this variant (for debugging and reproducibility).</summary>
    public IReadOnlyList<int>? OrderPermutation { get; init; }

    /// <summary>Evaluation score set by the simulation engine after calling <c>Evaluate()</c>.</summary>
    public ScheduleScore? Score { get; set; }

    internal ScheduleVariant(Dictionary<object, StepSnapshot> snapshots)
    {
        Snapshots = snapshots;
    }

    /// <summary>
    /// Writes this variant's computed schedule back into the <c>IProcessStep</c> objects.
    /// Call this only once the best solution has been identified.
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
