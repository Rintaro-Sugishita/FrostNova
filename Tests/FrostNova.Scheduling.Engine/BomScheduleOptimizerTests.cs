using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

public class BomScheduleOptimizerTests
{
    private readonly TimeSpan _unit = TimeSpan.FromHours(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0);

    private static IReadOnlyList<MyBom> CloneNodes(IReadOnlyList<MyBom> nodes)
    {
        // 深コピー：Start/End/LineId/MemberId をリセットしたクローンを返す
        return nodes.Select(n => new MyBom
        {
            Id = n.Id,
            Level = n.Level,
            Priority = n.Priority,
            ProcessSteps = n.ProcessSteps.Select(s => new MyStep
            {
                Id = s.Id,
                LeadTime = s.LeadTime,
                SetupTime = s.SetupTime,
                PostSetupTime = s.PostSetupTime,
                PreBuffer = s.PreBuffer,
                PostBuffer = s.PostBuffer,
                StepOrder = s.StepOrder,
                Quantity = s.Quantity,
                IsFixedTime = s.IsFixedTime,
                BatchCapacity = s.BatchCapacity,
                Lines = s.Lines.ToList(),
                Members = s.Members.ToList(),
                ScaleSetupByEfficiency = s.ScaleSetupByEfficiency,
                // Start/End/LineId/MemberId はリセット（デフォルト null）
            }).ToList(),
        }).ToList();
    }

    [Fact]
    public void Optimizer_ShouldReturnValidScore_ForSingleNode()
    {
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(2) };
        var node = new MyBom { Id = "N1", Priority = 0, ProcessSteps = new List<MyStep> { step } };
        var nodes = new List<MyBom> { node };

        var (bestNodes, bestScore, violations) = BomScheduleOptimizer.Optimize<MyBom, string, MyStep, string>(
            nodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base),
            true,
            _unit,
            CloneNodes,
            iterations: 5);

        Assert.NotNull(bestNodes);
        Assert.NotNull(bestScore);
        Assert.Empty(violations);
    }

    [Fact]
    public void Optimizer_Iterations0_ShouldReturnInitialSchedule()
    {
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1) };
        var node = new MyBom { Id = "N1", Priority = 0, ProcessSteps = new List<MyStep> { step } };
        var nodes = new List<MyBom> { node };

        var (bestNodes, bestScore, violations) = BomScheduleOptimizer.Optimize<MyBom, string, MyStep, string>(
            nodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base),
            true,
            _unit,
            CloneNodes,
            iterations: 0);

        Assert.NotNull(bestNodes);
        Assert.Single(bestNodes);
    }

    [Fact]
    public void Evaluator_ShouldReturnZeroPenalty_WhenNoViolations()
    {
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(1), WorkingSpan = TimeSpan.FromHours(1) };
        var weights = new ScheduleScoreWeights();
        var score = ScheduleEvaluator.Evaluate<MyStep, string>(
            new[] { step },
            Array.Empty<ScheduleViolationInfo>(),
            weights);

        Assert.Equal(0, score.DeadlinePenaltyMinutes);
        Assert.Equal(0, score.TotalScore - score.TotalLeadTimeMinutes * weights.LeadTimeWeight - 0 * weights.SetupWeight, 0.001);
    }

    [Fact]
    public void Evaluator_ShouldComputeDeadlinePenalty_ForViolations()
    {
        var stepId = "S1";
        var violation = new ScheduleViolationInfo(
            StepId: stepId,
            Type: ScheduleViolationType.DeadlineMiss,
            ConstraintDate: new DateTime(2026, 4, 1, 12, 0, 0),
            ActualDate: new DateTime(2026, 4, 1, 14, 0, 0)  // 2時間遅れ
        );

        var step = new MyStep { Id = stepId, LeadTime = TimeSpan.FromHours(2) };
        var weights = new ScheduleScoreWeights(DeadlineWeight: 1.0);
        var score = ScheduleEvaluator.Evaluate<MyStep, string>(
            new[] { step },
            new[] { violation },
            weights);

        // 2時間 = 120分のペナルティ
        Assert.Equal(120.0, score.DeadlinePenaltyMinutes, 1);
        Assert.True(score.TotalScore > 0);
    }
}
