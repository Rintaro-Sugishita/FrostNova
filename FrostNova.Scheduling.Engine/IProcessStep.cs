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

    /// <summary>数量（バッチサイズ計算用）</summary>
    double Quantity { get; }

    /// <summary>単位数量あたりの時間か、固定時間か</summary>
    bool IsFixedTime { get; }

    /// <summary>一度に処理できる最大数量（1なら個別、10なら10個まで同時）</summary>
    double BatchCapacity { get; }



    public object? LineId { get; set; }
    public object? MemberId { get; set; }
}
public interface IProcessStep<TKey, TMachine, TMember> : IProcessStep<TKey>, IResourceConstraint
{
}
