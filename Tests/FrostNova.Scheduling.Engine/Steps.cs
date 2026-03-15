using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine.Tests;

[DebuggerDisplay("Id: {Id}, Start: {Start}, End: {End}, Order: {StepOrder}, Lead: {LeadTime}")]
public class MyStep : IProcessStep<string>, IResourceConstraint
{
    public string Id { get; set; } = "";
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? LeadTime { get; set; }
    public int StepOrder { get; set; }
    public double Quantity { get; set; } = 1;
    public bool IsFixedTime { get; set; }
    public double BatchCapacity { get; set; } = 1;
    public TimeSpan SetupTime { get; set; } = TimeSpan.Zero;

    // リソース候補
    public List<(object? Id, double Efficiency)> Lines { get; set; } = new();
    public List<(object? Id, double Efficiency)> Members { get; set; } = new();
    public TimeSpan? WorkingSpan { get; set; }
    public object? LineId { get; set; }
    public object? MemberId { get; set; }

    public IEnumerable<(object? Id, double Efficiency)> GetLineCandidates() => Lines;
    public IEnumerable<(object? Id, double Efficiency)> GetMemberCandidates() => Members;
}

[DebuggerDisplay("Id: {Id}, Level: {Level}, Priority: {Priority}, Steps: {ProcessSteps.Count}")]
public class MyBom : IBomNode<MyBom,string, string, MyStep>
{
    public string Id { get; set; } = "";
    public int Level { get; set; }
    public int Priority { get; set; }
    public MyBom? Parent { get; set; }
    public List<MyBom> Children { get; set; } = new();
    public List<MyStep> ProcessSteps { get; set; } = new();

}


// --- テスト用補助クラス ---
public class MyRange : IPendingTimeRange
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? WorkingSpan { get; set; }

    // コンストラクタで初期値を入れられるようにしておくと便利
    public MyRange(DateTime? s = null, DateTime? e = null)
    {
        Start = s;
        End = e;
    }
}
