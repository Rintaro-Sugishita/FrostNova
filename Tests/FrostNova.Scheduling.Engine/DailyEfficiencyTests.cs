using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

/// <summary>
/// 日別（暦日単位）効率のテスト。
/// レートは工程の途中でも変動するため、稼働スライスごとに消化される仕事量を追って期待値を組み立てている。
/// </summary>
public class DailyEfficiencyTests
{
    private readonly TimeSpan _unit = TimeSpan.FromHours(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0); // 水曜 9:00

    private static readonly IEnumerable<IPendingTimeRange> NoNonWorking = Enumerable.Empty<IPendingTimeRange>();
    private static IEnumerable<IPendingTimeRange> NoResourceCalendar(BomScheduler.ResourceKey k) => Enumerable.Empty<IPendingTimeRange>();

    // --- 基本：レートが所要時間に効く ---

    [Fact]
    public void Forward_HalfRateDay_ShouldDoubleElapsed()
    {
        // 効率0.5 → 1スライス(1h)で 0.5h ぶんしか進まない。4h の作業に 8 スライス必要。
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(4) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(_base, step.Start);              // 9:00
        Assert.Equal(_base.AddHours(8), step.End);    // 17:00
    }

    [Fact]
    public void Forward_RateChangesAtMidnight_ConsumesAtEachDaysRate()
    {
        // 4/1 20:00 開始、作業 6h。4/1 は等倍、4/2 は 0.5 倍。
        // 20:00-24:00 の 4 スライスで 4h 消化（残り 2h）→ 4/2 は 0.5 倍なので 4 スライス必要 → 4/2 04:00 終了。
        var start = new DateTime(2026, 4, 1, 20, 0, 0);
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(6) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, start), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: d => d.Day == 1 ? 1.0 : 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(start, step.Start);
        Assert.Equal(new DateTime(2026, 4, 2, 4, 0, 0), step.End);
    }

    [Fact]
    public void Backward_RateChangesAtMidnight_MirrorsForward()
    {
        // 上の逆算版。4/2 04:00 納期から遡ると 4/1 20:00 開始になること。
        var deadline = new DateTime(2026, 4, 2, 4, 0, 0);
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(6) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, deadline), false, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: d => d.Day == 1 ? 1.0 : 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(deadline, step.End);
        Assert.Equal(new DateTime(2026, 4, 1, 20, 0, 0), step.Start);
    }

    // --- カレンダーとの組み合わせ ---

    [Fact]
    public void SpanningHoliday_HolidayNotConsumed_RateStillApplies()
    {
        // 11:00 開始、作業 2h、効率 0.5、12:00-13:00 は昼休み。
        // 11:00-12:00(0.5h) → 昼休みは飛ばす → 13:00-16:00 の 3 スライス(1.5h) で計 2h。
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(2) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch }, NoResourceCalendar,
            s => new(0, _base.AddHours(2)), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(_base.AddHours(2), step.Start);  // 11:00
        Assert.Equal(_base.AddHours(7), step.End);    // 16:00
        // 昼休みは消化されないので、稼働スライスは 4 本＝4h
        Assert.Equal(TimeSpan.FromHours(4), step.WorkingSpan);
    }

    // --- 全体係数 × リソース係数 × リソース効率 ---

    [Fact]
    public void GlobalAndResourceFactors_Multiply()
    {
        // 全体 0.5、LineA だけさらに 0.5（＝0.25 倍速）、LineB は全体のみ（0.5 倍速）。
        var stepA = new MyStep { Id = "A", LeadTime = TimeSpan.FromHours(1), Lines = new() { ("LineA", 1.0) } };
        var stepB = new MyStep { Id = "B", LeadTime = TimeSpan.FromHours(1), Lines = new() { ("LineB", 1.0) } };
        var nodeA = new MyBom { Id = "NA", ProcessSteps = new List<MyStep> { stepA } };
        var nodeB = new MyBom { Id = "NB", ProcessSteps = new List<MyStep> { stepB } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { nodeA, nodeB }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: (k, d) => Equals(k.Id, "LineA") ? 0.5 : 1.0);

        Assert.Equal(TimeSpan.FromHours(4), stepA.End - stepA.Start); // 1h / 0.25
        Assert.Equal(TimeSpan.FromHours(2), stepB.End - stepB.Start); // 1h / 0.5
    }

    [Fact]
    public void ResourceEfficiency_TimesDailyFactor_CancelsOut()
    {
        // 設備効率 2.0 × 日別 0.5 = 1.0 → 等倍になること
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(3), Lines = new() { ("Fast", 2.0) } };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(TimeSpan.FromHours(3), step.End - step.Start);
    }

    // --- 段取り時間との関係 ---

    [Fact]
    public void ScaleSetupFalse_SetupNotStretchedByDailyRate()
    {
        // ScaleSetupByEfficiency=false → 段取りは実時間 1h のまま、作業だけ 0.5 倍速で 2h
        var step = new MyStep
        {
            Id = "S1",
            SetupTime = TimeSpan.FromHours(1),
            LeadTime = TimeSpan.FromHours(1),
            ScaleSetupByEfficiency = false
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(_base.AddHours(1), step.Start);
        Assert.Equal(_base.AddHours(3), step.End);
    }

    [Fact]
    public void ScaleSetupTrue_SetupAlsoStretchedByDailyRate()
    {
        // ScaleSetupByEfficiency=true → 段取りも 0.5 倍速になり 2h かかる
        var step = new MyStep
        {
            Id = "S1",
            SetupTime = TimeSpan.FromHours(1),
            LeadTime = TimeSpan.FromHours(1),
            ScaleSetupByEfficiency = true
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(_base.AddHours(2), step.Start);
        Assert.Equal(_base.AddHours(4), step.End);
    }

    [Fact]
    public void SetupSpanningHoliday_PushesLeadStartPastBreak()
    {
        // 従来経路は leadStart を「枠の先頭 + 段取り」の単純加算で出すため、
        // 段取りが昼休みをまたぐと leadStart が昼休みの中（12:30）に落ちる。
        // レート方式では段取り区間もカレンダーを歩くので 13:30 になる。
        var unit = TimeSpan.FromMinutes(30);
        var start = new DateTime(2026, 4, 1, 11, 30, 0);
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep
        {
            Id = "S1",
            SetupTime = TimeSpan.FromHours(1),
            LeadTime = TimeSpan.FromHours(1),
            Lines = new() { ("L1", 1.0) }
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch }, NoResourceCalendar,
            s => new(0, start), true, unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 1.0,   // レート方式を有効にするだけ（等倍）
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(new DateTime(2026, 4, 1, 13, 30, 0), step.Start);
        Assert.Equal(new DateTime(2026, 4, 1, 14, 30, 0), step.End);
    }

    [Fact]
    public void Book_ReservesCalendarAwareOccupancy_WhenSetupSpansHoliday()
    {
        // 同一ラインの後続工程が、先行工程の「実占有範囲（段取り込み・休憩をまたいだ実時間）」の後ろに入ること。
        var unit = TimeSpan.FromMinutes(30);
        var start = new DateTime(2026, 4, 1, 11, 30, 0);
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));

        var s1 = new MyStep { Id = "S1", SetupTime = TimeSpan.FromHours(1), LeadTime = TimeSpan.FromHours(1), Lines = new() { ("L1", 1.0) } };
        var s2 = new MyStep { Id = "S2", LeadTime = TimeSpan.FromHours(1), Lines = new() { ("L1", 1.0) } };
        var n1 = new MyBom { Id = "N1", Priority = 0, ProcessSteps = new List<MyStep> { s1 } };
        var n2 = new MyBom { Id = "N2", Priority = 1, ProcessSteps = new List<MyStep> { s2 } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { n1, n2 }, Enumerable.Empty<MyBom>(), new[] { lunch }, NoResourceCalendar,
            s => new(0, start), true, unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 1.0,
            resourceDailyEfficiencyGetter: null);

        // S1 の実占有は 11:30-14:30（段取りが昼休みをまたぐ）。S2 はその後ろから。
        Assert.Equal(new DateTime(2026, 4, 1, 14, 30, 0), s2.Start);
        Assert.Equal(new DateTime(2026, 4, 1, 15, 30, 0), s2.End);
    }

    // --- 制約違反時の配置 ---

    [Fact]
    public void Deadline_WithDailyRate_ReportsDeadlineMiss_AndPlacesCalendarAware()
    {
        // B(Priority=0) が 9:00 に置かれ、A(Priority=1, 5h) の latestFinish が 9:00 になる → DeadlineMiss。
        // 04:00-06:00 が非稼働なので、9:00 から 5h 分を遡ると 02:00 開始になる（従来は単純減算で 04:00＝休み中）。
        var holiday = new MyRange(new(2026, 4, 1, 4, 0, 0), new(2026, 4, 1, 6, 0, 0));
        var sB = new MyStep { Id = "B", StepOrder = 2, LeadTime = TimeSpan.FromHours(1) };
        var sA = new MyStep { Id = "A", StepOrder = 1, LeadTime = TimeSpan.FromHours(5) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { sA, sB } };

        var violations = BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { holiday }, NoResourceCalendar,
            s => s.Id == "B"
                ? new BaseDateInfo(0, _base, BaseDateMode.ScheduledStart)
                : new BaseDateInfo(1, _base, BaseDateMode.ScheduledStart),
            true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 1.0,
            resourceDailyEfficiencyGetter: null);

        Assert.Single(violations);
        Assert.Equal(ScheduleViolationType.DeadlineMiss, violations[0].Type);
        Assert.Equal("A", violations[0].StepId);
        Assert.Equal(_base, sA.End);                                  // 9:00
        Assert.Equal(new DateTime(2026, 4, 1, 2, 0, 0), sA.Start);    // 5h 作業 + 2h 休み
    }

    // --- 異常値のクランプ（無限ループしないこと） ---

    [Fact]
    public void ZeroEfficiency_ShouldClamp_AndTerminate()
    {
        // 効率 0 は 0.01 にクランプされる（＝その日が休みになるわけではない）。
        // 1h の作業が 100 スライス前後で終わること＝ハングしないこと。
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => 0.0,
            resourceDailyEfficiencyGetter: null);

        Assert.InRange(step.End!.Value - step.Start!.Value, TimeSpan.FromHours(100), TimeSpan.FromHours(101));
    }

    [Fact]
    public void NaNEfficiency_ShouldClamp_AndTerminate()
    {
        // Math.Max(0.01, NaN) は NaN を返すため、NaN は明示的に潰す必要がある。
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: _ => double.NaN,
            resourceDailyEfficiencyGetter: null);

        Assert.InRange(step.End!.Value - step.Start!.Value, TimeSpan.FromHours(100), TimeSpan.FromHours(101));
    }

    // --- ISetupMatrix との併用 ---

    [Fact]
    public void SetupMatrix_FeedsSetupSegment_NotLeadSegment()
    {
        // 動的段取り 2h（ScaleSetupByEfficiency=false なので実時間）、作業 1h を 0.5 倍速で 2h。
        // 段取りがレートの影響を受けず、作業だけが伸びることを確認する。
        var matrix = new FixedSetupMatrix(TimeSpan.FromHours(2));
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1), SetupTime = TimeSpan.Zero, Lines = new() { ("L1", 1.0) } };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), NoNonWorking, NoResourceCalendar,
            s => new(0, _base), true, _unit,
            leadTimeOverride: null, setupMatrix: matrix,
            globalDailyEfficiencyGetter: _ => 0.5,
            resourceDailyEfficiencyGetter: null);

        Assert.Equal(_base.AddHours(2), step.Start);  // 段取り 2h は実時間
        Assert.Equal(_base.AddHours(4), step.End);    // 作業 1h が 0.5 倍速で 2h
    }

    private sealed class FixedSetupMatrix : ISetupMatrix
    {
        private readonly TimeSpan _setup;
        public FixedSetupMatrix(TimeSpan setup) => _setup = setup;
        public TimeSpan GetSetupTime(object? fromItemKey, object? toItemKey) => _setup;
    }

    // --- 後方互換：getter を渡さなければ従来経路と一致すること ---

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void NullGetters_ShouldMatchLegacyOverload(int scenario)
    {
        var (legacyNodes, legacyArgs) = BuildScenario(scenario);
        var (newNodes, newArgs) = BuildScenario(scenario);

        // 従来オーバーロード
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            legacyNodes, Enumerable.Empty<MyBom>(), legacyArgs.NonWorking, NoResourceCalendar,
            legacyArgs.BaseDateGetter, legacyArgs.IsForward, legacyArgs.Unit,
            leadTimeOverride: null, setupMatrix: null);

        // 新オーバーロード（getter は両方 null）
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            newNodes, Enumerable.Empty<MyBom>(), newArgs.NonWorking, NoResourceCalendar,
            newArgs.BaseDateGetter, newArgs.IsForward, newArgs.Unit,
            leadTimeOverride: null, setupMatrix: null,
            globalDailyEfficiencyGetter: null,
            resourceDailyEfficiencyGetter: null);

        var legacySteps = legacyNodes.SelectMany(n => n.ProcessSteps).ToList();
        var newSteps = newNodes.SelectMany(n => n.ProcessSteps).ToList();

        Assert.Equal(legacySteps.Count, newSteps.Count);
        for (int i = 0; i < legacySteps.Count; i++)
        {
            Assert.Equal(legacySteps[i].Start, newSteps[i].Start);
            Assert.Equal(legacySteps[i].End, newSteps[i].End);
            Assert.Equal(legacySteps[i].WorkingSpan, newSteps[i].WorkingSpan);
            Assert.Equal(legacySteps[i].LineId, newSteps[i].LineId);
        }
    }

    private sealed record ScenarioArgs(
        IEnumerable<IPendingTimeRange> NonWorking,
        Func<MyStep, BaseDateInfo> BaseDateGetter,
        bool IsForward,
        TimeSpan Unit);

    private (MyBom[] Nodes, ScenarioArgs Args) BuildScenario(int scenario)
    {
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));

        switch (scenario)
        {
            case 0: // 昼休みをまたぐ順算
                {
                    var s = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(2) };
                    var n = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s } };
                    return (new[] { n }, new(new[] { lunch }, _ => new(0, _base.AddHours(2)), true, _unit));
                }
            case 1: // 昼休みをまたぐ逆算
                {
                    var s = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(3) };
                    var n = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s } };
                    return (new[] { n }, new(new[] { lunch }, _ => new(0, _base.AddHours(6)), false, _unit));
                }
            case 2: // 段取り + 後段取り + 効率
                {
                    var s = new MyStep
                    {
                        Id = "S1",
                        SetupTime = TimeSpan.FromHours(1),
                        LeadTime = TimeSpan.FromHours(1),
                        PostSetupTime = TimeSpan.FromHours(1),
                        Lines = new() { ("FastLine", 2.0) },
                        ScaleSetupByEfficiency = true
                    };
                    var n = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s } };
                    return (new[] { n }, new(Enumerable.Empty<IPendingTimeRange>(), _ => new(0, _base), true, TimeSpan.FromMinutes(1)));
                }
            case 3: // バッチ容量（同時 2 本）
                {
                    var s1 = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1), BatchCapacity = 2, Lines = new() { ("L1", 1.0) } };
                    var s2 = new MyStep { Id = "S2", LeadTime = TimeSpan.FromHours(1), BatchCapacity = 2, Lines = new() { ("L1", 1.0) } };
                    var s3 = new MyStep { Id = "S3", LeadTime = TimeSpan.FromHours(1), BatchCapacity = 2, Lines = new() { ("L1", 1.0) } };
                    var n1 = new MyBom { Id = "N1", Priority = 0, ProcessSteps = new List<MyStep> { s1 } };
                    var n2 = new MyBom { Id = "N2", Priority = 1, ProcessSteps = new List<MyStep> { s2 } };
                    var n3 = new MyBom { Id = "N3", Priority = 2, ProcessSteps = new List<MyStep> { s3 } };
                    return (new[] { n1, n2, n3 }, new(Enumerable.Empty<IPendingTimeRange>(), _ => new(0, _base), true, _unit));
                }
            default: // 複数ライン候補から最速を選ぶ
                {
                    var s = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(4), Lines = new() { ("Slow", 1.0), ("Fast", 2.0) } };
                    var n = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s } };
                    return (new[] { n }, new(new[] { lunch }, _ => new(0, _base.AddHours(2)), true, _unit));
                }
        }
    }
}
