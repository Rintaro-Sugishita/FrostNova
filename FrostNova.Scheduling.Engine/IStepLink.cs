namespace FrostNova.Scheduling.Engine;

public interface IStepLink<TStepKey>
{
    /// <summary>ID of the predecessor (upstream) step.</summary>
    TStepKey FromStepId { get; }

    /// <summary>ID of the successor (downstream) step.</summary>
    TStepKey ToStepId { get; }

    /// <summary>Dependency type (FinishToStart, StartToStart, FinishToFinish, etc.).</summary>
    StepDependencyType Type { get; }

    /// <summary>Lag time added after the dependency constraint is satisfied.</summary>
    TimeSpan Lag { get; }
}
