using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FrostNova.Scheduling.Engine;
using FrostNova.TimeRangeChart.TimeRangeHelpers;
using FrostNova.TimeRangeChart.Wpf.TimeRangeHelpers;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FrostNova.Scheduling.Sample;

/// <summary>
/// 大量データのベンチマーク: 既定で 1000 タスク × 30 設備 × 100 日間。
/// 「最適化なしで1回流した結果（ベースライン）」と「順序最適化後の結果」を並べて、
/// 実行時間・メイクスパン・設備稼働バランス・納期遵守率がどう変わるかを比較する。
/// </summary>
public partial class ScenarioCViewModel : ObservableObject
{
    /// <summary>データ生成の乱数シード。固定しているので同じ設定なら毎回同じ問題が出る。</summary>
    private const int Seed = 20260401;

    private static readonly Brush[] StepColors =
    [
        MakeBrush(70,  130, 180),   // 工程1
        MakeBrush(180, 100, 60),    // 工程2
        MakeBrush(60,  160, 80),    // 工程3
        MakeBrush(150, 60,  180),   // 工程4
        MakeBrush(190, 170, 30),    // 工程5 以降
    ];

    private static Brush MakeBrush(byte r, byte g, byte b)
    {
        var br = new SolidColorBrush(Color.FromArgb(215, r, g, b));
        br.Freeze();
        return br;
    }

    /// <summary>実行時間の積み上げバーの色（生成 / スケジューリング / 最適化 / 描画）。</summary>
    private static readonly Brush[] PhaseBrushes =
    [
        MakeBrush(150, 150, 150),   // 生成
        MakeBrush(70,  130, 180),   // スケジューリング
        MakeBrush(150, 60,  180),   // 最適化
        MakeBrush(215, 130, 40),    // 描画
    ];

    // ─── 問題サイズ ─────────────────────────────────────────────────────────
    [ObservableProperty] private int _taskCount = 1000;
    [ObservableProperty] private int _lineCount = 30;
    [ObservableProperty] private int _horizonDays = 100;
    [ObservableProperty] private int _stepsPerProduct = 4;

    // ─── 実行設定 ───────────────────────────────────────────────────────────
    /// <summary>スケジューリングの最小時間単位（時間）。小さいほど精密だが遅い。</summary>
    [ObservableProperty] private int _unitHours = 1;

    /// <summary>探索方法: none / random / ga</summary>
    [ObservableProperty] private string _selectedStrategy = "random";

    /// <summary>ランダム探索の試行回数 / GA の世代数</summary>
    [ObservableProperty] private int _iterations = 10;

    /// <summary>日別効率（金曜は全体 0.8、各設備は立ち上がり期に低下）を有効にする</summary>
    [ObservableProperty] private bool _useDailyEfficiency;

    /// <summary>ガントチャートへの描画。タスク数が多いと描画自体が重くなるので切り離せるようにしてある。</summary>
    [ObservableProperty] private bool _renderChart = true;

    [ObservableProperty] private bool _isRunning;

    // ─── 結果 ───────────────────────────────────────────────────────────────
    [ObservableProperty] private string _statusText =
        "「▶ ベンチマーク実行」で開始します。既定設定（1000タスク / ランダム探索10回 / 単位1h）で 20 秒ほどかかります。" +
        "急ぐときは「最適化なし」にするか、時間単位を 4 にしてください。";
    [ObservableProperty] private ObservableCollection<BenchmarkMetricRow> _metrics = new();

    // ─── 実行時間の内訳 ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<PhaseTiming> _phaseTimings = new();
    [ObservableProperty] private string _totalTimeText = "";
    [ObservableProperty] private string _computeVsRenderText = "";
    [ObservableProperty] private bool _hasTimings;

    // ─── チャートデータ ─────────────────────────────────────────────────────
    [ObservableProperty] private ObservableCollection<ScheduleResultRow> _resultRows = new();
    [ObservableProperty] private ObservableCollection<FooterColumnViewModel> _footerItems = new();
    [ObservableProperty] private ObservableCollection<ITimeRangeRowIndex> _nonWorkingTimes = new();
    [ObservableProperty] private DateTime _chartStart = DateTime.Today;
    [ObservableProperty] private DateTime _chartEnd = DateTime.Today.AddDays(100);

    [RelayCommand]
    public async Task RunBenchmarkAsync()
    {
        if (IsRunning) return;
        IsRunning = true;
        try
        {
            var taskCount = Math.Max(1, TaskCount);
            var lineCount = Math.Max(1, LineCount);
            var horizonDays = Math.Max(1, HorizonDays);
            var stepsPerProduct = Math.Clamp(StepsPerProduct, 1, 10);
            var unit = TimeSpan.FromHours(Math.Max(1, UnitHours));
            var baseDate = DateTime.Today;
            var strategyKey = SelectedStrategy;
            var iterations = Math.Max(1, Iterations);
            var useDaily = UseDailyEfficiency;

            StatusText = $"生成中… ({taskCount} タスク / {lineCount} 設備 / {horizonDays} 日)";

            var result = await Task.Run(() => Run(
                taskCount, lineCount, horizonDays, stepsPerProduct,
                unit, baseDate, strategyKey, iterations, useDaily));

            StatusText = result.Summary;
            Metrics = new ObservableCollection<BenchmarkMetricRow>(result.Metrics);

            long chartDataMs = 0, wpfRenderMs = 0;
            if (RenderChart)
            {
                var sw = Stopwatch.StartNew();
                BuildChart(result.Nodes, result.LineIds, baseDate, horizonDays);
                sw.Stop();
                chartDataMs = sw.ElapsedMilliseconds;

                // ここまでは ObservableCollection を組み立てただけで、WPF はまだ何も描いていない。
                // 実際のレイアウト・描画はディスパッチャ上で後から走るため、
                // それらより低い優先度のコールバックが回ってくるまでを実描画時間として測る。
                wpfRenderMs = await MeasureRenderAsync();
            }
            else
            {
                ClearChart();
            }

            BuildTimings(result, chartDataMs, wpfRenderMs);
        }
        finally
        {
            IsRunning = false;
        }
    }

    /// <summary>
    /// WPF が実際にレイアウト・描画を終えるまでの時間を測る。
    /// ContextIdle は Render / Loaded より低い優先度なので、描画が片付いた後に回ってくる。
    /// </summary>
    /// <remarks>
    /// チャートが仮想化している場合、ここで測れるのは「画面に見えている分」の描画時間になる。
    /// また Application.Current が無い環境（ヘッドレス実行など）では 0 を返す。
    /// </remarks>
    private static async Task<long> MeasureRenderAsync()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null) return 0;

        var sw = Stopwatch.StartNew();
        await dispatcher.InvokeAsync(() => { }, DispatcherPriority.ContextIdle);
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    /// <summary>合計時間・積み上げバー・フェーズ別内訳を組み立てる。</summary>
    private void BuildTimings(RunResult result, long chartDataMs, long wpfRenderMs)
    {
        var renderMs = chartDataMs + wpfRenderMs;
        var computeMs = result.GenerateMs + result.ScheduleMs + result.OptimizeMs;
        var total = Math.Max(1, computeMs + renderMs);

        var rows = new List<PhaseTiming>
        {
            new("生成", result.GenerateMs, result.GenerateMs / (double)total, "問題データの組み立て", PhaseBrushes[0]),
            new("スケジューリング", result.ScheduleMs, result.ScheduleMs / (double)total, "最適化なしで 1 回", PhaseBrushes[1]),
        };
        if (result.OptimizeMs > 0)
        {
            rows.Add(new("最適化", result.OptimizeMs, result.OptimizeMs / (double)total,
                $"{result.SimulationCount} 回 / 1 回あたり {result.OptimizeMs / (double)Math.Max(1, result.SimulationCount):N0} ms",
                PhaseBrushes[2]));
        }
        if (RenderChart)
        {
            rows.Add(new("描画", renderMs, renderMs / (double)total,
                $"データ組み立て {chartDataMs:N0} + WPF 実描画 {wpfRenderMs:N0} ms", PhaseBrushes[3]));
        }

        PhaseTimings = new ObservableCollection<PhaseTiming>(rows);
        TotalTimeText = FormatDuration(total);
        ComputeVsRenderText = RenderChart
            ? $"計算 {computeMs:N0} ms ({computeMs / (double)total:P1})  /  描画 {renderMs:N0} ms ({renderMs / (double)total:P1})"
            : $"計算 {computeMs:N0} ms（描画は無効）";
        HasTimings = true;
    }

    private static string FormatDuration(long ms)
        => ms >= 10_000 ? $"{ms / 1000.0:N1} s"
         : ms >= 1_000 ? $"{ms / 1000.0:N2} s"
         : $"{ms:N0} ms";

    // ─── ベンチマーク本体（UI スレッド外で実行される） ───────────────────────

    private sealed record RunResult(
        SampleBom[] Nodes,
        string[] LineIds,
        string Summary,
        List<BenchmarkMetricRow> Metrics,
        long GenerateMs,
        long ScheduleMs,
        long OptimizeMs,
        int SimulationCount);

    private static RunResult Run(
        int taskCount, int lineCount, int horizonDays, int stepsPerProduct,
        TimeSpan unit, DateTime baseDate, string strategyKey, int iterations, bool useDaily)
    {
        var sw = Stopwatch.StartNew();
        var (nodes, lineIds) = BuildProblem(taskCount, lineCount, horizonDays, stepsPerProduct, baseDate);
        var nonWorking = GenerateWeekends(baseDate, baseDate.AddDays(horizonDays))
            .Cast<IPendingTimeRange>().ToList();
        sw.Stop();
        var genMs = sw.ElapsedMilliseconds;

        Func<DateTime, double>? globalDaily = null;
        Func<BomScheduler.ResourceKey, DateTime, double>? resourceDaily = null;
        if (useDaily)
        {
            // 金曜は全体的にペースが落ちる
            globalDaily = d => d.DayOfWeek == DayOfWeek.Friday ? 0.8 : 1.0;
            // 各設備は最初の 14 日が立ち上がり期（設備ごとに立ち上がりの速さが違う）
            resourceDaily = (key, d) =>
            {
                var elapsed = (d - baseDate.Date).TotalDays;
                if (elapsed >= 14) return 1.0;
                var idx = key.Id is string s && s.Length > 1 && int.TryParse(s[1..], out var i) ? i : 0;
                var floor = 0.6 + (idx % 5) * 0.05;
                return floor + (1.0 - floor) * (elapsed / 14.0);
            };
        }

        // ── ベースライン: 最適化なしで 1 回 ──
        sw.Restart();
        var baseViolations = BomScheduler.Schedule<SampleBom, string, SampleStep, string>(
            nodes, Enumerable.Empty<SampleBom>(), nonWorking,
            _ => Enumerable.Empty<IPendingTimeRange>(),
            _ => new BaseDateInfo(0, baseDate, BaseDateMode.ScheduledStart),
            isForward: true, unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: globalDaily,
            resourceDailyEfficiencyGetter: resourceDaily);
        sw.Stop();
        var baseMs = sw.ElapsedMilliseconds;
        var baseStats = Summarize(nodes, lineIds, baseDate, horizonDays, baseViolations.Count);

        // ── 最適化 ──
        Stats? optStats = null;
        long optMs = 0;
        int simCount = 0;

        if (strategyKey != "none")
        {
            IOptimizationStrategy strategy = strategyKey switch
            {
                "ga" => new GeneticAlgorithmStrategy(new GaParameters(
                            PopulationSize: Math.Max(4, iterations / 2),
                            Generations: Math.Max(2, iterations / 2),
                            MutationRate: 0.05,
                            Seed: Seed)),
                _ => new RandomSearchStrategy(iterations, seed: Seed),
            };
            simCount = strategyKey == "ga"
                ? Math.Max(4, iterations / 2) * Math.Max(2, iterations / 2)
                : iterations;

            sw.Restart();
            SimulationEngine.Optimize<SampleBom, string, SampleStep, string>(
                nodes, Enumerable.Empty<SampleBom>(), nonWorking,
                _ => Enumerable.Empty<IPendingTimeRange>(),
                _ => new BaseDateInfo(0, baseDate, BaseDateMode.ScheduledStart),
                isForward: true, unit,
                new ScoreWeights(A: 1, B: 0, C: 0, D: 1),
                strategy,
                setupMatrix: null,
                comparers: null,
                globalDailyEfficiencyGetter: globalDaily,
                resourceDailyEfficiencyGetter: resourceDaily);
            sw.Stop();
            optMs = sw.ElapsedMilliseconds;
            optStats = Summarize(nodes, lineIds, baseDate, horizonDays, violationCount: null);
        }

        // 時間は「実行時間」ブロックに一本化しているので、この表には品質の指標だけを載せる。
        var metrics = new List<BenchmarkMetricRow>
        {
            new("メイクスパン", $"{baseStats.MakespanDays:F1} 日 / {horizonDays} 日 ({baseStats.HorizonUsage:P0})",
                optStats == null ? "—" : $"{optStats.MakespanDays:F1} 日 / {horizonDays} 日 ({optStats.HorizonUsage:P0})"),
            new("設備平均稼働率（メイクスパン内）", $"{baseStats.AverageUtilization:P1}", optStats == null ? "—" : $"{optStats.AverageUtilization:P1}"),
            new("実作業時間 最小/最大", $"{baseStats.MinLineHours:N0} / {baseStats.MaxLineHours:N0} h",
                optStats == null ? "—" : $"{optStats.MinLineHours:N0} / {optStats.MaxLineHours:N0} h"),
            new("稼働バランス（標準偏差）", $"{baseStats.LineHoursStdDev:N1} h", optStats == null ? "—" : $"{optStats.LineHoursStdDev:N1} h"),
            new("希望納期の遅れ合計", $"{baseStats.TotalLatenessHours:N0} h", optStats == null ? "—" : $"{optStats.TotalLatenessHours:N0} h"),
            new("納期内に完了した製品", $"{baseStats.OnTimeRate:P1}", optStats == null ? "—" : $"{optStats.OnTimeRate:P1}"),
            new("制約違反", $"{baseStats.ViolationCount?.ToString("N0") ?? "—"} 件", "—"),
            new("未割り付けタスク", $"{baseStats.UnscheduledCount:N0} 件", optStats == null ? "—" : $"{optStats.UnscheduledCount:N0} 件"),
        };

        // エンジンが最小化しているスコア（A×納期遅れ + D×稼働バランス、A=D=1）に相当する値で優劣を判定する。
        if (optStats != null)
        {
            var baseObj = baseStats.TotalLatenessHours + baseStats.LineHoursStdDev;
            var optObj = optStats.TotalLatenessHours + optStats.LineHoursStdDev;
            var verdict = optObj < baseObj
                ? $"改善 {(1 - optObj / Math.Max(1e-9, baseObj)):P1}"
                : $"⚠ ベースラインより悪化（{(optObj / Math.Max(1e-9, baseObj) - 1):P1}）";
            metrics.Add(new BenchmarkMetricRow("目的関数 (遅れ+バランス)", $"{baseObj:N0}", $"{optObj:N0}  →  {verdict}"));
        }

        var products = Math.Max(1, taskCount / stepsPerProduct);
        var summary =
            $"{products:N0} 製品 × {stepsPerProduct} 工程 = {products * stepsPerProduct:N0} タスク / " +
            $"{lineCount} 設備 / {horizonDays} 日 / 単位 {unit.TotalHours:N0}h" +
            (useDaily ? " / 日別効率あり" : "");

        return new RunResult(nodes, lineIds, summary, metrics, genMs, baseMs, optMs, simCount);
    }

    // ─── 問題データの生成 ───────────────────────────────────────────────────

    private static (SampleBom[] Nodes, string[] LineIds) BuildProblem(
        int taskCount, int lineCount, int horizonDays, int stepsPerProduct, DateTime baseDate)
    {
        var rnd = new Random(Seed);
        var lineIds = Enumerable.Range(0, lineCount).Select(i => $"L{i:00}").ToArray();
        var products = Math.Max(1, taskCount / stepsPerProduct);

        // 100 日で 30 設備がおおよそ半分埋まる程度の負荷になるよう所要時間を調整する。
        // 稼働時間 = 設備数 × 平日日数 × 24h。その 50% を総作業量の目安とする。
        var workingDays = CountWorkingDays(baseDate, horizonDays);
        var capacityHours = (double)lineCount * workingDays * 24.0;
        var avgLeadHours = Math.Max(1.0, capacityHours * 0.5 / Math.Max(1, products * stepsPerProduct));
        var minLead = Math.Max(1, (int)(avgLeadHours * 0.4));
        var maxLead = Math.Max(minLead + 1, (int)(avgLeadHours * 1.6));

        // 希望納期は期間の 30%〜95% に散らしたうえで、製品インデックスとの対応をシャッフルする。
        // シャッフルしないと「Priority = p」の初期順序がそのまま納期順（EDD）になってしまい、
        // 初期解が最初からほぼ最適なせいで最適化の効果が見えなくなる。
        var targetOffsets = Enumerable.Range(0, products)
            .Select(i => horizonDays * (0.30 + 0.65 * (i / (double)Math.Max(1, products - 1))))
            .ToArray();
        for (int i = targetOffsets.Length - 1; i > 0; i--)
        {
            var j = rnd.Next(i + 1);
            (targetOffsets[i], targetOffsets[j]) = (targetOffsets[j], targetOffsets[i]);
        }

        var nodes = new SampleBom[products];
        for (int p = 0; p < products; p++)
        {
            var targetOffset = targetOffsets[p];
            var bom = new SampleBom
            {
                Id = $"P{p:0000}",
                Level = 0,
                Priority = p,
                TargetDate = baseDate.AddDays(targetOffset),
                Deadline = DateTime.MaxValue,   // ハード違反にはしない（全解が MaxValue になるのを避ける）
                EarliestStart = DateTime.MinValue,
                ImportanceWeight = 1.0,
            };

            for (int s = 0; s < stepsPerProduct; s++)
            {
                // 各工程は 3〜5 台の設備で実行可能。設備ごとに 0.8〜1.6 倍の効率差を持たせる。
                var candidateCount = Math.Min(lineCount, 3 + rnd.Next(3));
                var candidates = new List<(object? Id, double Efficiency)>(candidateCount);
                var used = new HashSet<int>();
                while (candidates.Count < candidateCount)
                {
                    var idx = rnd.Next(lineCount);
                    if (!used.Add(idx)) continue;
                    candidates.Add((lineIds[idx], 0.8 + rnd.NextDouble() * 0.8));
                }

                bom.ProcessSteps.Add(new SampleStep
                {
                    Id = $"P{p:0000}_S{s}",
                    StepLabel = $"P{p:0000}-{s + 1}",
                    ProductId = bom.Id,
                    StepOrder = s,
                    LeadTime = TimeSpan.FromHours(rnd.Next(minLead, maxLead + 1)),
                    Lines = candidates,
                });
            }
            nodes[p] = bom;
        }
        return (nodes, lineIds);
    }

    // ─── 集計 ───────────────────────────────────────────────────────────────

    private sealed record Stats(
        double MakespanDays,
        double HorizonUsage,
        double AverageUtilization,
        double MinLineHours,
        double MaxLineHours,
        double LineHoursStdDev,
        double TotalLatenessHours,
        double OnTimeRate,
        int UnscheduledCount,
        int? ViolationCount);

    private static Stats Summarize(
        SampleBom[] nodes, string[] lineIds, DateTime baseDate, int horizonDays, int? violationCount)
    {
        var steps = nodes.SelectMany(n => n.ProcessSteps).ToList();
        var scheduled = steps.Where(s => s.Start.HasValue && s.End.HasValue).ToList();

        var makespanDays = scheduled.Count == 0
            ? 0
            : (scheduled.Max(s => s.End!.Value) - baseDate).TotalDays;

        // 設備ごとの実作業時間。
        // (End - Start) は週末や休憩をまたいだぶんも含む「経過時間」なので稼働時間としては使えない
        // （実際、経過時間で集計すると稼働可能時間を超えて 100% を上回る）。
        // エンジンが返す WorkingSpan が段取りを除いた実作業時間そのもの。
        var byLine = lineIds.ToDictionary(id => id, _ => 0.0);
        foreach (var s in scheduled)
        {
            if (s.LineId is string lid && byLine.ContainsKey(lid))
                byLine[lid] += (s.WorkingSpan ?? (s.End!.Value - s.Start!.Value)).TotalHours;
        }
        var hours = byLine.Values.ToArray();
        var mean = hours.Length == 0 ? 0 : hours.Average();
        var stdDev = hours.Length == 0 ? 0 : Math.Sqrt(hours.Average(h => (h - mean) * (h - mean)));

        // 稼働率の分母はメイクスパン内の稼働可能時間。
        // 期間全体（100 日）を分母にすると、早く終わったときに「詰まり具合」ではなく
        // 「与えた枠をどれだけ消化したか」を見ることになり、実態と乖離する。
        var makespanWorkingHours = CountWorkingDays(baseDate, (int)Math.Ceiling(makespanDays)) * 24.0;
        var utilization = makespanWorkingHours <= 0 ? 0 : mean / makespanWorkingHours;

        // 与えられた期間のうちどれだけ使ったか（メイクスパン ÷ 期間）
        var horizonUsage = horizonDays <= 0 ? 0 : makespanDays / horizonDays;

        // 希望納期は各製品の最終工程で判定する
        double lateness = 0;
        int onTime = 0, judged = 0;
        foreach (var node in nodes)
        {
            var final = node.ProcessSteps.OrderByDescending(s => s.StepOrder).FirstOrDefault();
            if (final?.End == null) continue;
            judged++;
            var delta = (final.End.Value - node.TargetDate).TotalHours;
            if (delta > 0) lateness += delta; else onTime++;
        }

        return new Stats(
            makespanDays,
            horizonUsage,
            utilization,
            hours.Length == 0 ? 0 : hours.Min(),
            hours.Length == 0 ? 0 : hours.Max(),
            stdDev,
            lateness,
            judged == 0 ? 0 : onTime / (double)judged,
            steps.Count - scheduled.Count,
            violationCount);
    }

    private static int CountWorkingDays(DateTime baseDate, int horizonDays)
    {
        int n = 0;
        for (var d = baseDate.Date; d < baseDate.Date.AddDays(horizonDays); d = d.AddDays(1))
            if (d.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday)) n++;
        return n;
    }

    private static IEnumerable<NonWorkingPeriod> GenerateWeekends(DateTime from, DateTime to)
    {
        for (var d = from.Date; d < to.Date; d = d.AddDays(1))
            if (d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                yield return new NonWorkingPeriod(d, d.AddDays(1));
    }

    // ─── チャート構築 ───────────────────────────────────────────────────────

    private void ClearChart()
    {
        ResultRows = new ObservableCollection<ScheduleResultRow>();
        NonWorkingTimes = new ObservableCollection<ITimeRangeRowIndex>();
        FooterItems = new ObservableCollection<FooterColumnViewModel>();
    }

    private void BuildChart(SampleBom[] nodes, string[] lineIds, DateTime baseDate, int horizonDays)
    {
        var rowIndex = lineIds.Select((id, i) => (id, i)).ToDictionary(x => x.id, x => x.i);
        var rows = lineIds.Select(id => new ScheduleResultRow { TeamName = id }).ToArray();

        foreach (var step in nodes.SelectMany(n => n.ProcessSteps))
        {
            if (step.Start == null || step.End == null) continue;
            if (step.LineId is not string lid || !rowIndex.TryGetValue(lid, out var ri)) continue;

            rows[ri].Tasks.Add(new ScheduledTask(step.Start.Value, step.End.Value)
            {
                StepId = step.Id,
                PhaseLabel = step.StepLabel,
                Color = StepColors[Math.Min(step.StepOrder, StepColors.Length - 1)],
                RowIndex = ri,
            });
        }

        var allSteps = nodes.SelectMany(n => n.ProcessSteps).Where(s => s.End.HasValue).ToList();
        var maxEnd = allSteps.Count == 0 ? baseDate.AddDays(horizonDays) : allSteps.Max(s => s.End!.Value);

        ChartStart = baseDate.Date;
        ChartEnd = maxEnd.Date.AddDays(2);

        ResultRows = new ObservableCollection<ScheduleResultRow>(rows);

        // 週末の背景帯（行数 × 週末日数 ぶん生成されるので、行数が多いとここが一番重い）
        var nonWorking = new List<ITimeRangeRowIndex>();
        foreach (var period in GenerateWeekends(ChartStart, ChartEnd))
            for (int row = 0; row < lineIds.Length; row++)
                nonWorking.Add(new FixedTimeRange(period.Start!.Value, period.End!.Value) { RowIndex = row });
        NonWorkingTimes = new ObservableCollection<ITimeRangeRowIndex>(nonWorking);

        var footer = new List<FooterColumnViewModel>();
        for (var d = ChartStart.Date; d < ChartEnd.Date; d = d.AddDays(1))
            footer.Add(new FooterColumnViewModel { ColumnDate = d });
        FooterItems = new ObservableCollection<FooterColumnViewModel>(footer);
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
}

/// <summary>ベンチマーク結果 1 行（指標名・ベースライン・最適化後）。</summary>
public record BenchmarkMetricRow(string Label, string Baseline, string Optimized);

/// <summary>実行時間の 1 フェーズ。<paramref name="Fraction"/> は合計に対する比率（積み上げバーの幅）。</summary>
public record PhaseTiming(string Label, long Milliseconds, double Fraction, string Detail, Brush Color)
{
    public string TimeText => Milliseconds >= 1000 ? $"{Milliseconds / 1000.0:N2} s" : $"{Milliseconds:N0} ms";
    public string PercentText => Fraction.ToString("P1");
}
