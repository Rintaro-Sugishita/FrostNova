using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine.Tests;

public class CalendarElasticityTests
{
    private readonly TimeSpan _unit = TimeSpan.FromHours(1);
    private readonly DateTime _baseDate = new(2026, 4, 1, 9, 0, 0); // 水曜 9:00

    [Fact]
    public void Forward_ShouldExtendDuration_WhenSpanningNonWorking()
    {
        // 11:00開始、2時間作業。12:00-13:00が昼休み。
        // 期待値: 11:00 - 14:00 (End - Start = 3時間)
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "F1", LeadTime = TimeSpan.FromHours(2) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(2)), true, _unit); // 11:00開始

        Assert.Equal(_baseDate.AddHours(2), step.Start); // 11:00
        Assert.Equal(_baseDate.AddHours(5), step.End);   // 14:00
        // 【重要】実作業時間は2時間だが、経過時間は3時間であること
        Assert.Equal(TimeSpan.FromHours(3), step.End - step.Start);
    }

    [Fact]
    public void Backward_ShouldExtendDuration_WhenSpanningNonWorking()
    {
        // 15:00納期、2時間作業。12:00-13:00が昼休み。
        // 期待値: 12:00開始 - 15:00終了 (End - Start = 3時間)
        // ※もしバグがあれば、17:00-2h = 15:00開始 と誤認してしまう
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "B1", LeadTime = TimeSpan.FromHours(3) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(6)), false, _unit); // 15:00納期

        Assert.Equal(_baseDate.AddHours(6), step.End);   // 15:00
        Assert.Equal(_baseDate.AddHours(2), step.Start); // 11:00 (11:00-15:00の3h分を確保するため11:00まで遡る)
        Assert.Equal(TimeSpan.FromHours(3), step.WorkingSpan);
    }
    [Fact]
    public void Backward_CorrectlySpansLunchBreak()
    {
        // 15:00納期、2時間作業、12:00-13:00昼休み
        // 15:00 --(1h)--> 14:00 --(1h)--> 13:00 (ここで2時間確保)
        // 期待値: 13:00開始, 15:00終了
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "B_Simple", LeadTime = TimeSpan.FromHours(2) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(6)), false, _unit); // 15:00納期

        Assert.Equal(_baseDate.AddHours(6), step.End);   // 15:00
        Assert.Equal(_baseDate.AddHours(4), step.Start); // 13:00 (15時-2h = 13時)
    }

    [Fact]
    public void Backward_MustSpanAcrossNonWorkingTime()
    {
        // 15:00納期、4時間作業、12:00-13:00昼休み
        // 15:00 --(2h)--> 13:00 (2時間経過)
        // 13:00 --(休み)--> 12:00 (時間は遡るが作業時間は減らない)
        // 12:00 --(2h)--> 10:00 (さらに2時間遡って計4時間確保)
        // 期待値: 10:00開始, 15:00終了
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "B_Span", LeadTime = TimeSpan.FromHours(4) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(6)), false, _unit); // 15:00納期

        // 結果の検証
        Assert.Equal(_baseDate.AddHours(6), step.End);   // 15:00
        Assert.Equal(_baseDate.AddHours(1), step.Start); // 10:00
                                                         // 経過時間は5時間（4時間作業 + 1時間休み）
        Assert.Equal(TimeSpan.FromHours(5), step.End - step.Start);
    }
    [Fact]
    public void Backward_ShouldShiftStart_WhenDeadlineIsInsideNonWorking()
    {
        // 納期が「休み時間中」に設定された場合
        // 13:00納期（12-13時は休み）、1時間作業。
        // 期待値: 12:00終了ではなく、11:00終了-10:00開始になるべき
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "B2", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(4)), false, _unit); // 13:00納期

        // 12-13時は使えないので、実質的なEndは12:00、そこから1時間遡って11:00開始
        Assert.Equal(_baseDate.AddHours(3), step.End);   // 12:00
        Assert.Equal(_baseDate.AddHours(2), step.Start); // 11:00
    }

    [Fact]
    public void Forward_ShouldWait_WhenStartIsInsideNonWorking()
    {
        // 開始指示が「休み時間中」の場合
        // 12:30開始指示（12-13時は休み）、1時間作業。
        // 期待値: 13:00に着手し、14:00に終わるべき
        var lunch = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "F2", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { lunch },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(3).AddMinutes(30)), true, _unit); // 12:30開始指示

        Assert.Equal(_baseDate.AddHours(4), step.Start); // 13:00
        Assert.Equal(_baseDate.AddHours(5), step.End);   // 14:00
    }


    [Fact]
    public void Forward_ShouldSkipMultipleHolidays()
    {
        // --- 条件 ---
        // 開始指示: 9:00
        // 作業時間: 3時間
        // 休日1: 10:00 - 11:00 (1時間)
        // 休日2: 12:00 - 13:00 (1時間)
        // --- 期待される動き ---
        // 09:00 - 10:00 [稼働 1h]
        // 10:00 - 11:00 [休日 スキップ]
        // 11:00 - 12:00 [稼働 2h]
        // 12:00 - 13:00 [休日 スキップ]
        // 13:00 - 14:00 [稼働 3h 完了!]
        // 期待値: 9:00開始 -> 14:00終了 (経過5時間)

        var holidays = new[] {
        new MyRange(new(2026, 4, 1, 10, 0, 0), new(2026, 4, 1, 11, 0, 0)),
        new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0))
    };
        var step = new MyStep { Id = "F_Fragile", LeadTime = TimeSpan.FromHours(3) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), holidays,
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate), true, _unit); // 9:00開始

        Assert.Equal(_baseDate, step.Start);             // 09:00
        Assert.Equal(_baseDate.AddHours(5), step.End);   // 14:00
    }

    [Fact]
    public void Backward_ShouldSkipMultipleHolidays()
    {
        // --- 条件 ---
        // 納期: 14:00
        // 作業時間: 3時間
        // 休日1: 10:00 - 11:00 (1時間)
        // 休日2: 12:00 - 13:00 (1時間)
        // --- 期待される動き (逆算) ---
        // 14:00 --(1h)--> 13:00 [稼働 1h確保]
        // 13:00 --(休日)--> 12:00 [スキップ]
        // 12:00 --(1h)--> 11:00 [稼働 2h確保]
        // 11:00 --(休日)--> 10:00 [スキップ]
        // 10:00 --(1h)--> 09:00 [稼働 3h確保 完了!]
        // 期待値: 09:00開始 -> 14:00終了

        var holidays = new[] {
        new MyRange(new(2026, 4, 1, 10, 0, 0), new(2026, 4, 1, 11, 0, 0)),
        new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0))
    };
        var step = new MyStep { Id = "B_Fragile", LeadTime = TimeSpan.FromHours(3) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), holidays,
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(5)), false, _unit); // 14:00納期

        Assert.Equal(_baseDate.AddHours(5), step.End);   // 14:00
        Assert.Equal(_baseDate, step.Start);             // 09:00
    }
}
