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
    /// Gets the operational safety margin before the process.
    /// </summary>
    TimeSpan PreBuffer { get; } // 前工程バッファ（運用上の余裕）

    /// <summary>
    /// Gets the preparation time required on the resource before the actual work.
    /// </summary>
    TimeSpan SetupTime { get; } // 前段取り時間（型替え、準備など）

    /// <summary>
    /// Gets the duration of the actual work on the resource.
    /// </summary>
    TimeSpan? LeadTime { get; } // 作業時間（リソース占有の主作業）

    /// <summary>
    /// Gets the post-processing time required on the resource after the work.
    /// </summary>
    TimeSpan PostSetupTime { get; } // 後段取り時間（清掃、冷却など）

    /// <summary>
    /// Gets the operational safety margin after the process.
    /// </summary>
    TimeSpan PostBuffer { get; } // 後工程バッファ（次工程への受け渡し余裕）

    /// <summary>
    /// Gets the execution order of this step (1, 2, 3...).
    /// </summary>
    int StepOrder { get; }

    /// <summary>Production quantity used for batch-size calculations.</summary>
    double Quantity { get; }

    /// <summary><see langword="true"/> if <see cref="LeadTime"/> is a fixed duration; <see langword="false"/> if it scales per unit of quantity.</summary>
    bool IsFixedTime { get; }

    /// <summary>Maximum quantity that can be processed simultaneously (1 = one-at-a-time; 10 = up to 10 units in one batch).</summary>
    double BatchCapacity { get; }



    public object? LineId { get; set; }
    public object? MemberId { get; set; }

    /// <summary>
    /// When <see langword="true"/>, resource efficiency is also applied to <see cref="SetupTime"/> and
    /// <see cref="PostSetupTime"/> (divided by efficiency, same as <see cref="LeadTime"/>).
    /// </summary>
    bool ScaleSetupByEfficiency => false;
}
public interface IProcessStep<TKey, TMachine, TMember> : IProcessStep<TKey>, IResourceConstraint
{
}
