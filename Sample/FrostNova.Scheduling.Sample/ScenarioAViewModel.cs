using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Scheduling.Engine;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace FrostNova.Scheduling.Sample;

/// <summary>
/// 製造ラインスケジューリング: 効率の異なる3ラインに5製品の工程を割り付ける。
/// SimulationEngine.Optimize() で製品処理順序を最適化し、最良スコアのスケジュールを表示する。
/// </summary>
public partial class ScenarioAViewModel : ObservableObject
{
    private static readonly string[] LineIds = ["Line_A", "Line_B", "Line_C"];

    // 工程定義: (工程名, 製品別所要時間[h], 使用可能ライン[])
    // 加工: Line_A・B のみ（Line_C には加工設備がない）
    // 仕上: Line_B・C のみ（Line_A には仕上設備がない）
    private static readonly (string Label, int[] DurationByProduct, string[] EligibleLines)[] StepDefs =
    [
        ("成形", [4, 5, 3, 6, 4], ["Line_A", "Line_B", "Line_C"]),
        ("加工", [6, 8, 5, 7, 9], ["Line_A", "Line_B"]),
        ("品検", [2, 3, 2, 3, 2], ["Line_A", "Line_B", "Line_C"]),
        ("仕上", [3, 4, 2, 5, 3], ["Line_B", "Line_C"]),
    ];

    private static readonly Brush[] ProductColors =
    [
        MakeBrush(70,  130, 180),   // SteelBlue
        MakeBrush(180, 100, 60),    // Burnt Orange
        MakeBrush(60,  160, 80),    // Forest Green
        MakeBrush(150, 60,  180),   // Purple
        MakeBrush(190, 170, 30),    // Gold
    ];

    private static Brush MakeBrush(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromArgb(210, r, g, b));
        br.Freeze();
        return br;
    }

    // ─── 工程×ラインのスコアマトリクス（効率スライダー） ───────────────────
    [ObservableProperty]
    private ObservableCollection<LineScoreRow> _lineScores = new(
    [
        new LineScoreRow("Line_A", step0: true,  step1: true,  step2: true,  step3: false),
        new LineScoreRow("Line_B", step0: true,  step1: true,  step2: true,  step3: true),
        new LineScoreRow("Line_C", step0: true,  step1: false, step2: true,  step3: true),
    ]);

    // ─── 最適化設定 ─────────────────────────────────────────────────────────
    /// <summary>探索方法: none / exhaustive / random / ga</summary>
    [ObservableProperty] private string _selectedStrategy = "none";

    /// <summary>ランダム探索の試行回数 / GA の世代数</summary>
    [ObservableProperty] private int _iterations = 50;

    /// <summary>スコア重み A: 希望納期ズレ（時間）</summary>
    [ObservableProperty] private double _weightA = 1.0;

    /// <summary>スコア重み D: ライン稼働バランス（標準偏差[h]）</summary>
    [ObservableProperty] private double _weightD = 1.0;

    /// <summary>最適化結果のスコア表示文字列</summary>
    [ObservableProperty] private string _lastScoreText = "";

    // ─── チャートデータ ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ScheduleResultRow> _resultRows = new();
    [ObservableProperty] private ObservableCollection<FooterColumnViewModel> _footerItems = new();
    [ObservableProperty] private ObservableCollection<ITimeRangeRowIndex> _nonWorkingTimes = new();
    [ObservableProperty] private DateTime _chartStart = DateTime.Today;
    [ObservableProperty] private DateTime _chartEnd = DateTime.Today.AddDays(2);

    [RelayCommand]
    public async Task RunSchedulingAsync()
    {
        var baseDate = DateTime.Today.AddHours(9);
        var unit = TimeSpan.FromMinutes(1);

        // 昼休み（毎日 12:00-13:00）を 3 日分設定
        var lunchBreaks = Enumerable.Range(0, 3)
            .Select(d => (IPendingTimeRange)new NonWorkingPeriod(
                DateTime.Today.AddDays(d).AddHours(12),
                DateTime.Today.AddDays(d).AddHours(13)))
            .ToList();

        // UI スレッドで BOM・工程を組み立て（LineScores へのアクセスが必要）
        var allNodes = new List<SampleBom>();
        var stepWithProduct = new List<(SampleStep Step, int ProdIdx)>();

        for (int p = 0; p < 5; p++)
        {
            var productId = $"P{p + 1}";
            var bom = new SampleBom
            {
                Id = productId,
                Level = 0,
                Priority = p,
                TargetDate = baseDate.AddHours(32),
                Deadline = baseDate.AddDays(3),
                EarliestStart = DateTime.MinValue,
                ImportanceWeight = 1.0,
            };

            for (int s = 0; s < StepDefs.Length; s++)
            {
                var (label, durations, eligibleLines) = StepDefs[s];

                var lineCandidates = eligibleLines
                    .Select(lineId =>
                    {
                        var row = LineScores.First(r => r.LineName == lineId);
                        return ((object?)lineId, row.GetScore(s));
                    })
                    .ToList();

                var step = new SampleStep
                {
                    Id = $"{productId}_S{s + 1}",
                    StepLabel = $"{productId} {label}",
                    ProductId = productId,
                    StepOrder = s,
                    LeadTime = TimeSpan.FromHours(durations[p]),
                    Lines = lineCandidates,
                };
                bom.ProcessSteps.Add(step);
                stepWithProduct.Add((step, p));
            }
            allNodes.Add(bom);
        }

        LastScoreText = "実行中...";

        if (SelectedStrategy == "none")
        {
            // 最適化なし: バックグラウンドで 1 回スケジューリング
            await Task.Run(() =>
                BomScheduler.Schedule<SampleBom, string, SampleStep, string>(
                    allNodes,
                    Enumerable.Empty<SampleBom>(),
                    lunchBreaks,
                    _ => Enumerable.Empty<IPendingTimeRange>(),
                    _ => new BaseDateInfo(0, baseDate, BaseDateMode.ScheduledStart),
                    isForward: true,
                    unit));

            LastScoreText = "";
        }
        else
        {
            // 最適化: バックグラウンドで複数回シミュレーション
            var weights = new ScoreWeights(A: WeightA, B: 0, C: 0, D: WeightD);

            IOptimizationStrategy strategy = SelectedStrategy switch
            {
                "exhaustive" => new ExhaustiveStrategy(),
                "random"     => new RandomSearchStrategy(Iterations),
                "ga"         => new GeneticAlgorithmStrategy(
                                    new GaParameters(
                                        PopulationSize: Math.Max(10, Iterations / 5),
                                        Generations: Iterations,
                                        MutationRate: 0.05)),
                _            => new ExhaustiveStrategy(),
            };

            var best = await Task.Run(() =>
                SimulationEngine.Optimize<SampleBom, string, SampleStep, string>(
                    allNodes,
                    Enumerable.Empty<SampleBom>(),
                    lunchBreaks,
                    _ => Enumerable.Empty<IPendingTimeRange>(),
                    _ => new BaseDateInfo(0, baseDate, BaseDateMode.ScheduledStart),
                    isForward: true,
                    unit,
                    weights,
                    strategy));

            var score = best.Score;
            if (score != null)
            {
                if (score.IsHardViolated)
                    LastScoreText = "⚠ 絶対納期違反（解なし）";
                else
                    LastScoreText = $"スコア: {score.TotalScore:F1}  " +
                                    $"納期ズレ: {score.TargetDateDeviationHours:F1}h  " +
                                    $"稼働バランス偏差: {score.LineBalanceStdDevHours:F2}h";
            }
            else
            {
                LastScoreText = "";
            }
        }

        BuildResults(stepWithProduct);
    }

    private void BuildResults(List<(SampleStep Step, int ProdIdx)> stepWithProduct)
    {
        var rows = LineIds.Select(id => new ScheduleResultRow { TeamName = id }).ToList();
        var rowIndex = rows.Select((r, i) => (r.TeamName, i)).ToDictionary(x => x.TeamName, x => x.i);

        foreach (var (step, prodIdx) in stepWithProduct)
        {
            if (step.Start == null || step.End == null) continue;
            if (step.LineId is not string lineId || !rowIndex.TryGetValue(lineId, out var ri)) continue;

            rows[ri].Tasks.Add(new ScheduledTask(step.Start.Value, step.End.Value)
            {
                StepId = step.Id,
                PhaseLabel = step.StepLabel,
                Color = ProductColors[prodIdx],
                RowIndex = ri,
            });
        }

        ResultRows.Clear();
        foreach (var r in rows) ResultRows.Add(r);

        var allSteps = stepWithProduct.Select(x => x.Step);
        var minStart = allSteps.Where(x => x.Start.HasValue).Select(x => x.Start!.Value).DefaultIfEmpty(DateTime.Today).Min();
        var maxEnd   = allSteps.Where(x => x.End.HasValue).Select(x => x.End!.Value).DefaultIfEmpty(DateTime.Today.AddDays(1)).Max();
        ChartStart = minStart.Date;
        ChartEnd   = maxEnd.Date.AddDays(1);

        BuildFooterItems(ChartStart, ChartEnd);
    }

    private void BuildFooterItems(DateTime from, DateTime to)
    {
        var items = new List<FooterColumnViewModel>();
        for (var dt = from; dt < to; dt = dt.AddHours(1))
            items.Add(new FooterColumnViewModel { ColumnDate = dt });
        FooterItems = new ObservableCollection<FooterColumnViewModel>(items);
        RecalcFooter();
    }

    public void RecalcFooter()
    {
        var allTasks = ResultRows.SelectMany(r => r.Tasks).ToList();
        foreach (var col in FooterItems)
        {
            var next = col.ColumnDate.AddHours(1);
            col.TaskCount = allTasks.Count(t => t.StartTime < next && t.EndTime > col.ColumnDate);
        }
    }
}
