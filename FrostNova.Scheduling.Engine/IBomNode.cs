using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine;

public interface IBomNode<TSelf, TNodeKey, TStepKey, TOperation>
    where TSelf : IBomNode<TSelf,TNodeKey, TStepKey, TOperation>
    where TOperation : IProcessStep<TStepKey>
    where TNodeKey : notnull
{
    /// <summary>
    /// Gets the unique identifier for this process step.
    /// </summary>
    TNodeKey Id { get; }

    TSelf? Parent { get; set; }

    List<TSelf> Children { get; set; }

    List<TOperation> ProcessSteps { get; set; }

    int Level { get; set; }

    int Priority { get; set; }

    /// <summary>
    /// ノード内の工程間のリンク（SS/FF/SF 依存）。
    /// デフォルトは空（FinishToStart のみ使用する場合は実装不要）。
    /// </summary>
    IEnumerable<IStepLink<TStepKey>> StepLinks => Enumerable.Empty<IStepLink<TStepKey>>();

}
