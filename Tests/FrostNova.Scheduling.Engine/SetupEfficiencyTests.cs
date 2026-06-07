using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

public class SetupEfficiencyTests
{
    private readonly TimeSpan _unit = TimeSpan.FromMinutes(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0);

    [Fact]
    public void Should_NotScaleSetupTime_When_ScaleSetupByEfficiencyIsFalse()
    {
        // 効率2.0でも ScaleSetupByEfficiency=false なら段取り時間は変わらない
        var step = new MyStep
        {
            Id = "S1",
            LeadTime = TimeSpan.FromHours(1),
            SetupTime = TimeSpan.FromHours(1),
            PostSetupTime = TimeSpan.FromHours(1),
            Lines = new() { ("FastLine", 2.0) },
            ScaleSetupByEfficiency = false
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // LeadTime 1h / 2.0 = 30min。Setup 1h + Lead 30min + PostSetup 1h = 2.5h 占有
        // Start (Lead開始) = _base + 1h(setup), End (Lead終了) = Start + 30min
        Assert.Equal(_base.AddHours(1), step.Start);
        Assert.Equal(_base.AddHours(1).AddMinutes(30), step.End);
    }

    [Fact]
    public void Should_ScaleSetupTime_When_ScaleSetupByEfficiencyIsTrue()
    {
        // 効率2.0 で ScaleSetupByEfficiency=true → 段取り時間も半分になる
        var step = new MyStep
        {
            Id = "S1",
            LeadTime = TimeSpan.FromHours(1),
            SetupTime = TimeSpan.FromHours(1),
            PostSetupTime = TimeSpan.FromHours(1),
            Lines = new() { ("FastLine", 2.0) },
            ScaleSetupByEfficiency = true
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // Setup 30min + Lead 30min + PostSetup 30min = 90min 占有
        // Start = _base + 30min (effectiveSetup), End = Start + 30min (effectiveLead)
        Assert.Equal(_base.AddMinutes(30), step.Start);
        Assert.Equal(_base.AddHours(1), step.End);
    }

    [Fact]
    public void ScaleSetup_ShouldStartEarlier_ThanNoScale_GivenSameEfficiency()
    {
        // ScaleSetupByEfficiency=true の方が占有時間が短く、早く終わる
        var run = (bool scale) =>
        {
            var step = new MyStep
            {
                Id = "S1",
                LeadTime = TimeSpan.FromHours(2),
                SetupTime = TimeSpan.FromHours(2),
                Lines = new() { ("FastLine", 2.0) },
                ScaleSetupByEfficiency = scale
            };
            var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };
            BomScheduler.Schedule<MyBom, string, MyStep, string>(
                new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
                k => Enumerable.Empty<IPendingTimeRange>(),
                s => new(0, _base), true, _unit);
            return step.End;
        };

        var endWithScale = run(true);
        var endWithoutScale = run(false);

        Assert.True(endWithScale < endWithoutScale,
            $"ScaleSetup=true should end earlier. true={endWithScale}, false={endWithoutScale}");
    }
}
