namespace FrostNova.Scheduling.Engine;

public interface IStepLink<TStepKey>
{
    /// <summary>依存元（先行）工程のID</summary>
    TStepKey FromStepId { get; }

    /// <summary>依存先（後続）工程のID</summary>
    TStepKey ToStepId { get; }

    /// <summary>依存の種別</summary>
    StepDependencyType Type { get; }

    /// <summary>遅延時間（ラグ）</summary>
    TimeSpan Lag { get; }
}
