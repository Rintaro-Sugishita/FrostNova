using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace FrostNova.Scheduling.Engine.Tests;

public class BatchCapacityTests
{
    private readonly TimeSpan _unit = TimeSpan.FromHours(1);
    private readonly DateTime _base = new(2026, 4, 1, 9, 0, 0);

    [Fact]
    public void Batch_NonOverlapping_ShouldNotCollide_WhenCapacityIsTwo()
    {
        // capacity=2 のリソースに連続した2予約（重複しない）があっても、
        // その間を含む新規ジョブは入れるべき（同時稼働は最大1件 < capacity）
        var line = "BatchLine";

        // 既存予約1: 9:00-10:00（LineId を明示的にセット）
        var e1 = new MyStep
        {
            Id = "E1", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            LineId = line,  // Book() が参照するのはこちら
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(1), WorkingSpan = TimeSpan.FromHours(1)
        };
        var refNode1 = new MyBom { Id = "R1", ProcessSteps = new List<MyStep> { e1 } };

        // 既存予約2: 10:00-11:00
        var e2 = new MyStep
        {
            Id = "E2", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            LineId = line,
            BatchCapacity = 2,
            Start = _base.AddHours(1), End = _base.AddHours(2), WorkingSpan = TimeSpan.FromHours(1)
        };
        var refNode2 = new MyBom { Id = "R2", ProcessSteps = new List<MyStep> { e2 } };

        // 新規: 9:00-11:00 の2時間ジョブ (capacity=2 なので同時1件なら入れる)
        var newStep = new MyStep
        {
            Id = "NEW", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) },
            BatchCapacity = 2
        };
        var target = new MyBom { Id = "T", ProcessSteps = new List<MyStep> { newStep } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { target },
            new[] { refNode1, refNode2 },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // 同時稼働は最大1件 < capacity=2 なので 9:00 から開始できる
        Assert.Equal(_base, newStep.Start);
        Assert.Equal(_base.AddHours(2), newStep.End);
    }

    [Fact]
    public void Batch_Simultaneous_ShouldCollide_WhenCapacityExceeded()
    {
        // capacity=2 のリソースに同時稼働2件がある → 3件目はブロックされる
        var line = "BatchLine";

        var e1 = new MyStep
        {
            Id = "E1", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) },
            LineId = line,
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(2), WorkingSpan = TimeSpan.FromHours(2)
        };
        var refNode1 = new MyBom { Id = "R1", ProcessSteps = new List<MyStep> { e1 } };

        var e2 = new MyStep
        {
            Id = "E2", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) },
            LineId = line,
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(2), WorkingSpan = TimeSpan.FromHours(2)
        };
        var refNode2 = new MyBom { Id = "R2", ProcessSteps = new List<MyStep> { e2 } };

        // 3件目: capacity=2 なのでブロックされ、11:00 以降になるはず
        var newStep = new MyStep
        {
            Id = "NEW", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            BatchCapacity = 2
        };
        var target = new MyBom { Id = "T", ProcessSteps = new List<MyStep> { newStep } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { target },
            new[] { refNode1, refNode2 },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // 2件同時稼働 → capacity=2 に達しているため 11:00 以降に押し出される
        Assert.True(newStep.Start >= _base.AddHours(2),
            $"Expected start >= 11:00, but was {newStep.Start}");
    }

    [Fact]
    public void Batch_ExclusiveResource_ShouldAlwaysBlock()
    {
        // capacity=1（排他）のリソースは重なりを即ブロック
        var line = "ExclusiveLine";

        var existing = new MyStep
        {
            Id = "E1", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) },
            LineId = line,
            BatchCapacity = 1,
            Start = _base, End = _base.AddHours(2), WorkingSpan = TimeSpan.FromHours(2)
        };
        var refNode = new MyBom { Id = "R", ProcessSteps = new List<MyStep> { existing } };

        var newStep = new MyStep
        {
            Id = "NEW", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            BatchCapacity = 1
        };
        var target = new MyBom { Id = "T", ProcessSteps = new List<MyStep> { newStep } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { target },
            new[] { refNode },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // 排他なので既存2時間が終わった後 11:00 から開始
        Assert.Equal(_base.AddHours(2), newStep.Start);
    }

    [Fact]
    public void Batch_PartialOverlap_CanFitInSlotWithLowerConcurrency()
    {
        // E1: 9-11, E2: 10-12 → 10-11 で同時2件（capacity=2 に達する）
        // 新規1h: 9-10 は E1 のみで concurrent=1 < 2 なので 9:00 に開始できる
        var line = "BatchLine";

        var e1 = new MyStep
        {
            Id = "E1", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) }, LineId = line,
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(2), WorkingSpan = TimeSpan.FromHours(2)
        };
        var e2 = new MyStep
        {
            Id = "E2", LeadTime = TimeSpan.FromHours(2),
            Lines = new() { (line, 1.0) }, LineId = line,
            BatchCapacity = 2,
            Start = _base.AddHours(1), End = _base.AddHours(3), WorkingSpan = TimeSpan.FromHours(2)
        };
        var refNode1 = new MyBom { Id = "R1", ProcessSteps = new List<MyStep> { e1 } };
        var refNode2 = new MyBom { Id = "R2", ProcessSteps = new List<MyStep> { e2 } };

        var newStep = new MyStep
        {
            Id = "NEW", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            BatchCapacity = 2
        };
        var target = new MyBom { Id = "T", ProcessSteps = new List<MyStep> { newStep } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { target },
            new[] { refNode1, refNode2 },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // 9-10 の区間は concurrent=1 < capacity=2 なので新規は 9:00 に開始できる
        Assert.Equal(_base, newStep.Start);
        Assert.Equal(_base.AddHours(1), newStep.End);
    }

    [Fact]
    public void Batch_FullOverlap_ShouldBlockUntilCapacityFrees()
    {
        // E1: 9-12 (3h), E2: 9-12 (3h) → 9-12 全域で同時2件（capacity=2 に達する）
        // 新規: 9:00 から capacity=2 で試みると全域でブロック → 12:00 以降
        var line = "BatchLine";

        var e1 = new MyStep
        {
            Id = "E1", LeadTime = TimeSpan.FromHours(3),
            Lines = new() { (line, 1.0) }, LineId = line,
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(3), WorkingSpan = TimeSpan.FromHours(3)
        };
        var e2 = new MyStep
        {
            Id = "E2", LeadTime = TimeSpan.FromHours(3),
            Lines = new() { (line, 1.0) }, LineId = line,
            BatchCapacity = 2,
            Start = _base, End = _base.AddHours(3), WorkingSpan = TimeSpan.FromHours(3)
        };
        var refNode1 = new MyBom { Id = "R1", ProcessSteps = new List<MyStep> { e1 } };
        var refNode2 = new MyBom { Id = "R2", ProcessSteps = new List<MyStep> { e2 } };

        var newStep = new MyStep
        {
            Id = "NEW", LeadTime = TimeSpan.FromHours(1),
            Lines = new() { (line, 1.0) },
            BatchCapacity = 2
        };
        var target = new MyBom { Id = "T", ProcessSteps = new List<MyStep> { newStep } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { target },
            new[] { refNode1, refNode2 },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _base), true, _unit);

        // 9-12 全域で concurrent=2 = capacity=2 → 12:00 以降に押し出される
        Assert.True(newStep.Start >= _base.AddHours(3),
            $"Expected start >= 12:00, but was {newStep.Start}");
    }
}
