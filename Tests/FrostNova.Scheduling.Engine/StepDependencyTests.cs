using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

/// <summary>
/// IStepLink を使った SS / FF / SF 依存のテスト
/// </summary>
public class StepDependencyTests
{
    private readonly TimeSpan _unit = TimeSpan.FromHours(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0);

    private static MyStepLink MakeLink(string from, string to, StepDependencyType type, TimeSpan lag = default)
        => new() { FromStepId = from, ToStepId = to, Type = type, Lag = lag };

    [Fact]
    public void SS_ZeroLag_ShouldAlignStartTimes()
    {
        // S1 と S2 が SS リンク（ラグ0）→ S2 は S1 開始と同時に開始できる
        var s1 = new MyStep { Id = "S1", StepOrder = 1, LeadTime = TimeSpan.FromHours(3) };
        var s2 = new MyStep { Id = "S2", StepOrder = 2, LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom
        {
            Id = "N1",
            ProcessSteps = new List<MyStep> { s1, s2 },
            StepLinks = new[] { MakeLink("S1", "S2", StepDependencyType.StartToStart) }
        };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // SS: S2.Start >= S1.Start + lag(0) → S2 は S1 と同時または後に開始
        Assert.True(s2.Start >= s1.Start, $"S2.Start({s2.Start}) should be >= S1.Start({s1.Start})");
    }

    [Fact]
    public void SS_WithLag_ShouldOffsetStartByLag()
    {
        // S1 と S2 が SS + lag=2h → S2 は S1 開始の2h後に開始
        var s1 = new MyStep { Id = "S1", StepOrder = 1, LeadTime = TimeSpan.FromHours(4) };
        var s2 = new MyStep { Id = "S2", StepOrder = 2, LeadTime = TimeSpan.FromHours(2) };
        var node = new MyBom
        {
            Id = "N1",
            ProcessSteps = new List<MyStep> { s1, s2 },
            StepLinks = new[] { MakeLink("S1", "S2", StepDependencyType.StartToStart, TimeSpan.FromHours(2)) }
        };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // SS+2h: S2.Start >= S1.Start + 2h
        Assert.True(s2.Start >= s1.Start!.Value.AddHours(2),
            $"S2.Start({s2.Start}) should be >= S1.Start+2h({s1.Start!.Value.AddHours(2)})");
    }

    [Fact]
    public void FS_WithLag_ShouldAddLagAfterPredecessorEnd()
    {
        // FS + lag=1h → S2 は S1 終了の1h後に開始
        var s1 = new MyStep { Id = "S1", StepOrder = 1, LeadTime = TimeSpan.FromHours(2) };
        var s2 = new MyStep { Id = "S2", StepOrder = 2, LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom
        {
            Id = "N1",
            ProcessSteps = new List<MyStep> { s1, s2 },
            StepLinks = new[] { MakeLink("S1", "S2", StepDependencyType.FinishToStart, TimeSpan.FromHours(1)) }
        };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // FS+1h: S2.Start >= S1.End + 1h
        Assert.True(s2.Start >= s1.End!.Value.AddHours(1),
            $"S2.Start({s2.Start}) should be >= S1.End+1h({s1.End!.Value.AddHours(1)})");
    }

    [Fact]
    public void FF_ZeroLag_ShouldConstrainSuccessorFinish()
    {
        // FF + lag=0 → S2 は S1 の終了以降に終了しなければならない
        var s1 = new MyStep { Id = "S1", StepOrder = 1, LeadTime = TimeSpan.FromHours(3) };
        var s2 = new MyStep { Id = "S2", StepOrder = 2, LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom
        {
            Id = "N1",
            ProcessSteps = new List<MyStep> { s1, s2 },
            StepLinks = new[] { MakeLink("S1", "S2", StepDependencyType.FinishToFinish) }
        };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // FF: S2.End >= S1.End + lag(0) → constraint propagated
        // S2 の開始は S1.End 以降に押し出される
        Assert.True(s2.Start >= s1.End,
            $"S2.Start({s2.Start}) should be >= S1.End({s1.End}) due to FF constraint");
    }
}

// テスト用 IStepLink 実装
public class MyStepLink : IStepLink<string>
{
    public string FromStepId { get; set; } = "";
    public string ToStepId { get; set; } = "";
    public StepDependencyType Type { get; set; }
    public TimeSpan Lag { get; set; }
}
