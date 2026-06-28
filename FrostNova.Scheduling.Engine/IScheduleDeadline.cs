namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Optional interface that adds desired-date, hard-deadline, and earliest-start constraints to a BOM node.
/// When an <c>IBomNode</c> implementation also implements this interface, the simulation engine includes
/// it in score evaluation.
/// </summary>
public interface IScheduleDeadline
{
    /// <summary>Desired completion date. Deviations from this date contribute a penalty to the evaluation score.</summary>
    DateTime TargetDate { get; }

    /// <summary>
    /// Absolute deadline. Any scenario where the last operation finishes after this date is treated as a
    /// hard violation and excluded from optimization.
    /// </summary>
    DateTime Deadline { get; }

    /// <summary>
    /// Earliest allowable start date (inventory floor).
    /// An inventory penalty is incurred when any operation is scheduled before this date.
    /// Represents warehouse capacity or cash-flow constraints.
    /// </summary>
    DateTime EarliestStart { get; }

    /// <summary>
    /// Importance multiplier for this product or customer (default 1.0).
    /// The target-date deviation penalty is scaled by this factor.
    /// For example, a value of 2.0 means one hour late incurs twice the penalty of an item with weight 1.0.
    /// </summary>
    double ImportanceWeight { get; }

    /// <summary>
    /// Risk buffer coefficient applied to lead time during scheduling (default 0.0).
    /// The effective lead time is computed as <c>LeadTime × (1 + RiskBuffer)</c>.
    /// For example, 0.2 extends a 2-hour task to 2.4 hours, leaving slack after the actual work.
    /// </summary>
    double RiskBuffer { get; }
}
