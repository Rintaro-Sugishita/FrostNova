using CommunityToolkit.Mvvm.ComponentModel;
using FrostNova.Scheduling.Engine;
using FrostNova.TimeRangeChart;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FrostNova.Scheduling.Sample;

// ─── Scheduling Engine Models ──────────────────────────────────────────────

public class SampleStep : IProcessStep<string>, IResourceConstraint
{
    public string Id { get; set; } = "";
    public string StepLabel { get; set; } = "";
    public string ProductId { get; set; } = "";

    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? WorkingSpan { get; set; }

    public TimeSpan PreBuffer { get; set; } = TimeSpan.Zero;
    public TimeSpan SetupTime { get; set; } = TimeSpan.Zero;
    public TimeSpan? LeadTime { get; set; }
    public TimeSpan PostSetupTime { get; set; } = TimeSpan.Zero;
    public TimeSpan PostBuffer { get; set; } = TimeSpan.Zero;
    public int StepOrder { get; set; }
    public double Quantity { get; set; } = 1;
    public bool IsFixedTime { get; set; } = true;
    public double BatchCapacity { get; set; } = 1;

    public List<(object? Id, double Efficiency)> Lines { get; set; } = new();
    public List<(object? Id, double Efficiency)> Members { get; set; } = new();

    public object? LineId { get; set; }
    public object? MemberId { get; set; }

    public IEnumerable<(object? Id, double Efficiency)> GetLineCandidates() => Lines;
    public IEnumerable<(object? Id, double Efficiency)> GetMemberCandidates() => Members;
}

public class SampleBom : IBomNode<SampleBom, string, string, SampleStep>, IScheduleDeadline
{
    public string Id { get; set; } = "";
    public int Level { get; set; }
    public int Priority { get; set; }
    public SampleBom? Parent { get; set; }
    public List<SampleBom> Children { get; set; } = new();
    public List<SampleStep> ProcessSteps { get; set; } = new();

    // IScheduleDeadline — デフォルトは「制約なし」
    public DateTime TargetDate { get; set; } = DateTime.MaxValue;
    public DateTime Deadline { get; set; } = DateTime.MaxValue;
    public DateTime EarliestStart { get; set; } = DateTime.MinValue;
    public double ImportanceWeight { get; set; } = 1.0;
    public double RiskBuffer { get; set; } = 0.0;
}

public class NonWorkingPeriod : IPendingTimeRange
{
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
    public TimeSpan? WorkingSpan { get; set; }

    public NonWorkingPeriod(DateTime start, DateTime end)
    {
        Start = start;
        End = end;
    }
}

// ─── Chart / Result Models ─────────────────────────────────────────────────

public partial class ScheduledTask : TimeRange
{
    public string StepId { get; set; } = "";
    public string PhaseLabel { get; set; } = "";
    public Brush Color { get; set; } = Brushes.SteelBlue;

    public ScheduledTask(DateTime start, DateTime end) : base(start, end)
    {
        CanDragTasksBetweenRows = false;
        CanStack = false;
        StackBaseTop = 4;
    }
}

public class ScheduleResultRow
{
    public string TeamName { get; set; } = "";
    public ObservableCollection<ScheduledTask> Tasks { get; set; } = new();
}

public partial class FooterColumnViewModel : ObservableObject
{
    public DateTime ColumnDate { get; set; }

    [ObservableProperty]
    private int _taskCount;
}

/// <summary>工程種別ごとの、あるラインのスコア（=効率）を保持する行</summary>
public partial class LineScoreRow : ObservableObject
{
    public string LineName { get; }
    // Eligible[i] = true なら、その工程インデックスをこのラインで実行可能
    public bool[] Eligible { get; }

    [ObservableProperty] private double _step0Score = 1.0;
    [ObservableProperty] private double _step1Score = 1.0;
    [ObservableProperty] private double _step2Score = 1.0;
    [ObservableProperty] private double _step3Score = 1.0;

    public LineScoreRow(string name, bool step0, bool step1, bool step2, bool step3)
    {
        LineName = name;
        Eligible = [step0, step1, step2, step3];
    }

    public double GetScore(int stepIdx) => stepIdx switch
    {
        0 => Step0Score,
        1 => Step1Score,
        2 => Step2Score,
        3 => Step3Score,
        _ => 1.0,
    };
}
