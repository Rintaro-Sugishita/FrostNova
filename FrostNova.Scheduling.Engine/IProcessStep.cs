using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine;

public interface IProcessStep<TKey> : IPendingTimeRange
{
    /// <summary>
    /// Gets the unique identifier for this process step.
    /// </summary>
    TKey Id { get;}

    /// <summary>
    /// Gets the duration of the step in days.
    /// </summary>
    TimeSpan? LeadTime { get; }

    /// <summary>
    /// Gets the execution order of this step (1, 2, 3...).
    /// </summary>
    int StepOrder { get; }

    /// <summary>数量（バッチサイズ計算用）</summary>
    double Quantity { get; }

    /// <summary>単位数量あたりの時間か、固定時間か</summary>
    bool IsFixedTime { get; }

    /// <summary>一度に処理できる最大数量（1なら個別、10なら10個まで同時）</summary>
    double BatchCapacity { get; }

    /// <summary>前工程との段取り時間</summary>
    TimeSpan SetupTime { get; }

    public object? LineId { get; set; }
    public object? MemberId { get; set; }
}
public interface IProcessStep<TKey, TMachine, TMember> : IProcessStep<TKey>, IResourceConstraint
{
}
