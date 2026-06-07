namespace FrostNova.Scheduling.Engine;

public enum StepDependencyType
{
    /// <summary>前工程の終了後に後工程が開始（デフォルト）</summary>
    FinishToStart = 0,
    /// <summary>前工程の開始後に後工程が開始</summary>
    StartToStart,
    /// <summary>前工程の終了後に後工程が終了</summary>
    FinishToFinish,
    /// <summary>前工程の開始後に後工程が終了（稀なケース）</summary>
    StartToFinish,
}
