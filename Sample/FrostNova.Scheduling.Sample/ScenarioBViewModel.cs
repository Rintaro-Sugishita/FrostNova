using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Scheduling.Engine;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FrostNova.Scheduling.Sample;

/// <summary>
/// ウォーターフォール型システム開発スケジューリング（2プロジェクト競合版）。
/// Project Alpha（優先度高）と Project Beta（優先度低）が同一チームリソースを共有するため、
/// リソース競合とキューイングをガントチャートで可視化できる。
/// チーム効率スライダーで「効率を上げるとスケジュールがどう変わるか」を確認できる。
/// </summary>
public partial class ScenarioBViewModel : ObservableObject
{
    private static readonly string[] TeamIds = ["Team_PM", "Team_Design", "Team_FE", "Team_BE", "Team_QA"];

    // Alpha プロジェクトのチームカラー（濃い目）
    private static readonly Dictionary<string, Brush> AlphaColors = new()
    {
        ["Team_PM"]     = MakeBrush(40,  120, 60,  220),
        ["Team_Design"] = MakeBrush(60,  80,  180, 220),
        ["Team_FE"]     = MakeBrush(200, 70,  40,  220),
        ["Team_BE"]     = MakeBrush(140, 50,  180, 220),
        ["Team_QA"]     = MakeBrush(160, 140, 20,  220),
    };

    // Beta プロジェクトのチームカラー（同系統の薄め）
    private static readonly Dictionary<string, Brush> BetaColors = new()
    {
        ["Team_PM"]     = MakeBrush(100, 190, 130, 180),
        ["Team_Design"] = MakeBrush(110, 150, 220, 180),
        ["Team_FE"]     = MakeBrush(240, 130, 100, 180),
        ["Team_BE"]     = MakeBrush(190, 110, 220, 180),
        ["Team_QA"]     = MakeBrush(210, 200, 80,  180),
    };

    private static Brush MakeBrush(byte r, byte g, byte b, byte a = 255)
    {
        var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
        br.Freeze();
        return br;
    }

    // ─── チーム効率スライダー ─────────────────────────────────────
    [ObservableProperty] private double _teamPmEfficiency    = 1.0;
    [ObservableProperty] private double _teamDesignEfficiency = 1.2;
    [ObservableProperty] private double _teamFeEfficiency    = 1.0;
    [ObservableProperty] private double _teamBeEfficiency    = 1.5;
    [ObservableProperty] private double _teamQaEfficiency    = 1.0;

    // ─── チャートデータ ─────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ScheduleResultRow> _resultRows = new();
    [ObservableProperty] private ObservableCollection<FooterColumnViewModel> _footerItems = new();
    [ObservableProperty] private ObservableCollection<ITimeRangeRowIndex> _nonWorkingTimes = new();
    [ObservableProperty] private DateTime _chartStart = DateTime.Today;
    [ObservableProperty] private DateTime _chartEnd = DateTime.Today.AddDays(60);

    [RelayCommand]
    public void RunScheduling()
    {
        var baseDate = DateTime.Today;
        var unit = TimeSpan.FromDays(1);

        var globalNonWorking = GenerateWeekends(baseDate, baseDate.AddDays(120))
            .Cast<IPendingTimeRange>().ToList();

        // BOM レベル体系：
        //   Alpha: req=14, basic=12, detail=10, dev=8, test=6, uat=4, rel=2
        //   Beta:  req=13, basic=11, detail= 9, dev=7, test=5,        rel=3
        // 同フェーズでは Alpha が常に先 → リソース競合時に Alpha が優先される

        // ─── Project Alpha ─────────────────────────────────────────
        var aReq    = MakeBom("α:要件定義",  "alpha_req",    14, "Team_PM",     TeamPmEfficiency,    4);
        var aBasic  = MakeBom("α:基本設計",  "alpha_basic",  12, "Team_Design", TeamDesignEfficiency, 5);
        var aDetail = MakeBom("α:詳細設計",  "alpha_detail", 10, "Team_Design", TeamDesignEfficiency, 6);
        var aDev    = MakeBomDual("α:開発",  "alpha_dev",     8,
            ("α:FE開発", "Team_FE", TeamFeEfficiency, 9),
            ("α:BE開発", "Team_BE", TeamBeEfficiency, 9));
        var aTest   = MakeBom("α:結合テスト", "alpha_test",    6, "Team_QA",     TeamQaEfficiency,    5);
        var aUat    = MakeBom("α:UAT",        "alpha_uat",     4, "Team_PM",     TeamPmEfficiency,    3);
        var aRel    = MakeBom("α:リリース",   "alpha_rel",     2, "Team_PM",     TeamPmEfficiency,    1);

        Link(aReq, aBasic); Link(aBasic,  aDetail); Link(aDetail, aDev);
        Link(aDev, aTest);  Link(aTest,   aUat);    Link(aUat,    aRel);

        // ─── Project Beta ──────────────────────────────────────────
        var bReq    = MakeBom("β:要件定義",  "beta_req",     13, "Team_PM",     TeamPmEfficiency,    3);
        var bBasic  = MakeBom("β:基本設計",  "beta_basic",   11, "Team_Design", TeamDesignEfficiency, 4);
        var bDetail = MakeBom("β:詳細設計",  "beta_detail",   9, "Team_Design", TeamDesignEfficiency, 5);
        var bDev    = MakeBomDual("β:開発",  "beta_dev",      7,
            ("β:FE開発", "Team_FE", TeamFeEfficiency, 7),
            ("β:BE開発", "Team_BE", TeamBeEfficiency, 7));
        var bTest   = MakeBom("β:結合テスト", "beta_test",     5, "Team_QA",     TeamQaEfficiency,    4);
        var bRel    = MakeBom("β:リリース",   "beta_rel",      3, "Team_PM",     TeamPmEfficiency,    1);

        Link(bReq, bBasic); Link(bBasic, bDetail); Link(bDetail, bDev);
        Link(bDev, bTest);  Link(bTest,  bRel);

        var allNodes = new SampleBom[]
        {
            aReq, aBasic, aDetail, aDev, aTest, aUat, aRel,
            bReq, bBasic, bDetail, bDev, bTest, bRel,
        };

        BomScheduler.Schedule<SampleBom, string, SampleStep, string>(
            allNodes,
            Enumerable.Empty<SampleBom>(),
            globalNonWorking,
            _ => Enumerable.Empty<IPendingTimeRange>(),
            _ => new BaseDateInfo(0, baseDate, BaseDateMode.ScheduledStart),
            isForward: true,
            unit);

        BuildResults(allNodes);
    }

    private static SampleBom MakeBom(string label, string id, int level, string teamId, double eff, int days)
    {
        var bom = new SampleBom { Id = id, Level = level };
        bom.ProcessSteps.Add(new SampleStep
        {
            Id = $"{id}_step",
            StepLabel = label,
            StepOrder = 0,
            LeadTime = TimeSpan.FromDays(days),
            Lines = [((object?)teamId, eff)],
        });
        return bom;
    }

    private static SampleBom MakeBomDual(string label, string id, int level,
        (string Label, string TeamId, double Eff, int Days) fe,
        (string Label, string TeamId, double Eff, int Days) be)
    {
        var bom = new SampleBom { Id = id, Level = level };
        bom.ProcessSteps.Add(new SampleStep
        {
            Id = $"{id}_fe", StepLabel = fe.Label, StepOrder = 0,
            LeadTime = TimeSpan.FromDays(fe.Days),
            Lines = [((object?)fe.TeamId, fe.Eff)],
        });
        bom.ProcessSteps.Add(new SampleStep
        {
            Id = $"{id}_be", StepLabel = be.Label, StepOrder = 1,
            LeadTime = TimeSpan.FromDays(be.Days),
            Lines = [((object?)be.TeamId, be.Eff)],
        });
        return bom;
    }

    private static void Link(SampleBom child, SampleBom parent)
    {
        child.Parent = parent;
        parent.Children.Add(child);
    }

    private void BuildResults(SampleBom[] allNodes)
    {
        var rows = TeamIds.Select(id => new ScheduleResultRow { TeamName = id }).ToList();
        var rowIndex = rows.Select((r, i) => (r.TeamName, i)).ToDictionary(x => x.TeamName, x => x.i);

        foreach (var step in allNodes.SelectMany(n => n.ProcessSteps))
        {
            if (step.Start == null || step.End == null || step.LineId is not string lineId) continue;
            if (!rowIndex.TryGetValue(lineId, out var ri)) continue;

            var isAlpha = step.Id.StartsWith("alpha");
            var colorMap = isAlpha ? AlphaColors : BetaColors;
            var color = colorMap.GetValueOrDefault(lineId, Brushes.Gray);

            rows[ri].Tasks.Add(new ScheduledTask(step.Start.Value, step.End.Value)
            {
                StepId = step.Id,
                PhaseLabel = step.StepLabel,
                Color = color,
                RowIndex = ri,
            });
        }

        ResultRows.Clear();
        foreach (var r in rows) ResultRows.Add(r);

        var allSteps = allNodes.SelectMany(n => n.ProcessSteps).ToList();
        var minStart = allSteps.Where(x => x.Start.HasValue).Select(x => x.Start!.Value).DefaultIfEmpty(DateTime.Today).Min();
        var maxEnd   = allSteps.Where(x => x.End.HasValue).Select(x => x.End!.Value).DefaultIfEmpty(DateTime.Today.AddDays(60)).Max();
        ChartStart = minStart.Date.AddDays(-1);
        ChartEnd   = maxEnd.Date.AddDays(2);

        BuildNonWorkingVisual();
        // フッターを ChartStart/ChartEnd 基準で生成（tick と一致させる）
        BuildFooterItems(ChartStart, ChartEnd);
    }

    private void BuildNonWorkingVisual()
    {
        NonWorkingTimes.Clear();
        foreach (var period in GenerateWeekends(ChartStart, ChartEnd))
        {
            for (int row = 0; row < TeamIds.Length; row++)
            {
                NonWorkingTimes.Add(new FixedTimeRange(period.Start!.Value, period.End!.Value)
                {
                    RowIndex = row,
                });
            }
        }
    }

    private void BuildFooterItems(DateTime from, DateTime to)
    {
        var items = new List<FooterColumnViewModel>();
        for (var d = from.Date; d < to.Date; d = d.AddDays(1))
            items.Add(new FooterColumnViewModel { ColumnDate = d });
        FooterItems = new ObservableCollection<FooterColumnViewModel>(items);
        RecalcFooter();
    }

    public void RecalcFooter()
    {
        var allTasks = ResultRows.SelectMany(r => r.Tasks).ToList();
        foreach (var col in FooterItems)
        {
            var next = col.ColumnDate.AddDays(1);
            col.TaskCount = allTasks.Count(t => t.StartTime.Date < next && t.EndTime.Date > col.ColumnDate.Date);
        }
    }

    private static IEnumerable<NonWorkingPeriod> GenerateWeekends(DateTime from, DateTime to)
    {
        for (var d = from.Date; d < to.Date; d = d.AddDays(1))
        {
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                yield return new NonWorkingPeriod(d, d.AddDays(1));
        }
    }
}
