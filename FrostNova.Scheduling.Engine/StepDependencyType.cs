namespace FrostNova.Scheduling.Engine;

public enum StepDependencyType
{
    /// <summary>Successor starts after predecessor finishes (default).</summary>
    FinishToStart = 0,
    /// <summary>Successor starts after predecessor starts.</summary>
    StartToStart,
    /// <summary>Successor finishes after predecessor finishes.</summary>
    FinishToFinish,
    /// <summary>Successor finishes after predecessor starts (rare).</summary>
    StartToFinish,
}
