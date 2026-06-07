using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

public class ViolationReportingTests
{
    private readonly TimeSpan _unit = TimeSpan.FromMinutes(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0);

    [Fact]
    public void Should_ReturnEmptyViolations_When_StepFitsWithinConstraints()
    {
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        var violations = BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        Assert.Empty(violations);
    }

    [Fact]
    public void Should_ReportDeadlineMiss_When_StepCannotFitBeforeDownstreamStep()
    {
        // B (Priority=0, StepOrder=2) が先にスケジュールされ _base に配置される。
        // A (Priority=1, StepOrder=1) は後から順算されるが、
        // B.Start = _base が latestFinish となるため 5h の A は入らない → DeadlineMiss
        var deadline = _base;
        var s_b = new MyStep { Id = "B", StepOrder = 2, LeadTime = TimeSpan.FromMinutes(1) };
        var s_a = new MyStep { Id = "A", StepOrder = 1, LeadTime = TimeSpan.FromHours(5) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s_a, s_b } };

        var violations = BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => s.Id == "B"
                ? new BaseDateInfo(0, deadline, BaseDateMode.ScheduledStart)
                : new BaseDateInfo(1, deadline, BaseDateMode.ScheduledStart),
            true, _unit);

        Assert.Single(violations);
        Assert.Equal(ScheduleViolationType.DeadlineMiss, violations[0].Type);
        Assert.Equal("A", violations[0].StepId);
    }

    [Fact]
    public void Should_ReportEarliestStartViolation_When_BackwardStepConstrainedByPredecessor()
    {
        // A (Priority=0, StepOrder=1, Deadline) が先にスケジュールされ [deadline-2h, deadline] に配置。
        // B (Priority=1, StepOrder=2, Deadline) は後から逆算されるが、
        // A.End = deadline が earliestStart となるため 8h の B は deadline 以降から遡れない
        var deadline = _base.AddHours(10);
        var s_a = new MyStep { Id = "A", StepOrder = 1, LeadTime = TimeSpan.FromHours(2) };
        var s_b = new MyStep { Id = "B", StepOrder = 2, LeadTime = TimeSpan.FromHours(8) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s_a, s_b } };

        var violations = BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => s.Id == "A"
                ? new BaseDateInfo(0, deadline, BaseDateMode.Deadline)
                : new BaseDateInfo(1, deadline, BaseDateMode.Deadline),
            false, _unit);

        Assert.Single(violations);
        Assert.Equal(ScheduleViolationType.EarliestStartViolation, violations[0].Type);
        Assert.Equal("B", violations[0].StepId);
    }

    [Fact]
    public void ViolationInfo_ShouldContainStepId_And_DatesInViolation()
    {
        var deadline = _base;
        var s_b = new MyStep { Id = "B", StepOrder = 2, LeadTime = TimeSpan.FromMinutes(1) };
        var s_a = new MyStep { Id = "A", StepOrder = 1, LeadTime = TimeSpan.FromHours(5) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s_a, s_b } };

        var violations = BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => s.Id == "B"
                ? new BaseDateInfo(0, deadline, BaseDateMode.ScheduledStart)
                : new BaseDateInfo(1, deadline, BaseDateMode.ScheduledStart),
            true, _unit);

        Assert.NotEmpty(violations);
        var v = violations[0];
        Assert.NotNull(v.StepId);
        // 制約日 <= 実際日（遅れているから違反）
        Assert.True(v.ActualDate >= v.ConstraintDate,
            $"ActualDate({v.ActualDate}) should be >= ConstraintDate({v.ConstraintDate})");
    }
}
