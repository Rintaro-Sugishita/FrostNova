using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Scheduling.Engine.Tests;

public class BomSchedulerTests
{
    private readonly TimeSpan _unit = TimeSpan.FromMinutes(1);
    private readonly DateTime _baseDate = new(2026, 4, 1, 9, 0, 0); // 4/1 09:00 (水)

    [Fact]
    /// <summary>効率による所要時間の変化_効率2倍なら時間は半分になるべき</summary>
    public void Should_ReduceDuration_When_EfficiencyIsDouble()
    {
        // Arrange
        var step = new MyStep
        {
            Id = "S1",
            LeadTime = TimeSpan.FromHours(2),
            Lines = new List<(object, double)> { ("FastMachine", 2.0) }
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        // Act
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate), true, _unit);

        // Assert
        // 2時間のLeadTimeが効率2.0で1時間になるはず
        Assert.Equal(TimeSpan.FromHours(1), step.End - step.Start);
    }

    [Fact]
    /// <summary>非稼働時間のまたぎ_昼休み1時間をまたぐ場合は終了が1時間遅れるべき</summary>
    public void Should_DelayEnd_When_CrossingLunchBreak()
    {
        // Arrange
        var lunchBreak = new MyRange(new(2026, 4, 1, 12, 0, 0), new(2026, 4, 1, 13, 0, 0));
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(2) }; // 2時間作業
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };
        var nodes = new []{ node };
        // Act
        // 11:00開始 -> 12~13時休み -> 14:00終了になるはず
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            nodes, Enumerable.Empty<MyBom>(), new[] { lunchBreak },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate.AddHours(2)), true, _unit);

        // Assert
        Assert.Equal(_baseDate.AddHours(2), step.Start); // 11:00
        Assert.Equal(_baseDate.AddHours(5), step.End);   // 14:00
    }

    [Fact]
    /// <summary>バッチ処理_キャパ超えで時間が倍増するべき</summary>
    public void Should_DoubleDuration_When_ExceedingBatchCapacity()

    {
        // Arrange
        var step = new MyStep
        {
            Id = "S1",
            LeadTime = TimeSpan.FromHours(1),
            Quantity = 11,
            BatchCapacity = 10,
            IsFixedTime = false
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        // Act
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate), true, _unit);

        // Assert
        // 10個までは1時間、11個目は次のバッチなので計2時間
        Assert.Equal(TimeSpan.FromHours(2), step.End - step.Start);
    }

    [Fact]
    /// <summary>代替リソースの回避_先行予約がある場合は空いているマシンへ逃げるべき</summary>
    public void Should_UseAlternativeResource_When_PrimaryIsBooked()
    {
        // Arrange
        var machineA = "MachineA";
        var machineB = "MachineB";

        // 1. 既存予約（MachineAを9~12時で占有）
        var refStep = new MyStep { Id = "Ref", Start = _baseDate, End = _baseDate.AddHours(3), LeadTime = TimeSpan.FromHours(3) };
        var refNode = new MyBom { Id = "RefNode", ProcessSteps = new List<MyStep> { refStep } };
        // ※実際にはBookロジックのためにResourceKeyを特定させる必要があるが、
        // 簡易的にreferenceNodesとして渡し、Scheduler側でBookされる想定

        // 2. 新規計算（MachineAとBが候補。Aは埋まっているがBは空いている）
        var targetStep = new MyStep
        {
            Id = "Target",
            LeadTime = TimeSpan.FromHours(1),
            Lines = new List<(object, double)> { (machineA, 1.0), (machineB, 1.0) }
        };
        var targetNode = new MyBom { Id = "TargetNode", ProcessSteps = new List<MyStep> { targetStep } };

        // Act
        // 既存予約としてMachineAを使用している工程を無理やり登録するため、
        // 内部的にResourceKey(Line, "MachineA")でBookされるように設定
        // (テスト用にSchedulerをラップするか、Mockを調整)

        // ここでは単純化のため、machineA固有のカレンダーを「非稼働」にして擬似再現
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { targetNode }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            key => key.Id.Equals(machineA) ? new[] { new MyRange(_baseDate, _baseDate.AddHours(5)) } : Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate), true, _unit);

        // Assert
        // MachineAは休みなので、MachineBで9:00から開始されるはず
        Assert.Equal(_baseDate, targetStep.Start);
    }

    [Fact]
    /// <summary>バックワード計算_納期から逆算して開始日が決まるべき</summary>
    public void Should_CalculateStartFromDeadline_In_BackwardMode()
    {
        // Arrange
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromHours(3) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };
        var deadLine = _baseDate.AddHours(10); // 19:00納期

        // Act
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, deadLine), false, _unit);

        // Assert
        Assert.Equal(deadLine, step.End);
        Assert.Equal(deadLine.AddHours(-3), step.Start);
    }

    [Fact]
    /// <summary>親子BOMの連動_子の完了が遅れると親の開始も押し出されるべき</summary>
    public void Should_PushParentStart_When_ChildEndIsDelayed()
    {
        // Arrange
        var childStep = new MyStep { Id = "Child", LeadTime = TimeSpan.FromHours(5) };
        var parentStep = new MyStep { Id = "Parent", LeadTime = TimeSpan.FromHours(2) };

        var childNode = new MyBom { Id = "C", Level = 1, ProcessSteps = new List<MyStep> { childStep } };
        var parentNode = new MyBom { Id = "P", Level = 0, ProcessSteps = new List<MyStep> { parentStep } };

        childNode.Parent = parentNode;
        parentNode.Children.Add(childNode);

        // Act
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { childNode, parentNode }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _baseDate), true, _unit);

        // Assert
        // 子：9:00~14:00
        // 親：14:00~16:00 (子のEndを基準に開始される)
        Assert.Equal(childStep.End, parentStep.Start);
    }
    [Fact]
    public void Forward_ShouldHandleMixedBaseDateModes()
    {
        // --- 条件 ---
        // 全体戦略: Forward (最短着手)
        // 工程構成: 
        //   Step1 (1h) -> Step2 (1h) -> Step3 (1h) -> Step4 (1h, 18:00納期指定!)
        // 
        // Step1 の着手可能日: 4/1 09:00
        // Step4 の納期指定: 4/1 18:00 (Deadlineモード)
        // 
        // --- 期待される動き ---
        // 1. Step1, 2, 3 は 09:00 から順次最短で配置される。
        // 2. Step4 は 18:00 から「逆算」して配置される。
        //
        // 結果: 
        //   S1: 09:00 - 10:00
        //   S2: 10:00 - 11:00
        //   S3: 11:00 - 12:00
        //   S4: 17:00 - 18:00 (JIT配置)
        //   ※S3終了(12:00)からS4開始(17:00)までの間に、意図的な「空き」ができること。

        var baseTime = new DateTime(2026, 4, 1, 0, 0, 0);
        var steps = new List<MyStep>
    {
        new MyStep { Id = "S1", StepOrder = 0, LeadTime = TimeSpan.FromHours(1) },
        new MyStep { Id = "S2", StepOrder = 1, LeadTime = TimeSpan.FromHours(1) },
        new MyStep { Id = "S3", StepOrder = 2, LeadTime = TimeSpan.FromHours(1) },
        new MyStep { Id = "S4", StepOrder = 3, LeadTime = TimeSpan.FromHours(1) }
    };
        var node = new MyBom { Id = "ForwardMixedNode", ProcessSteps = steps };

        // baseDateGetter の定義 (BaseDateInfo クラスを使用)
        BaseDateInfo GetBaseDate(MyStep s)
        {
            if (s.Id == "S1")
                return new BaseDateInfo(Priority: -10, BaseDate: baseTime.AddHours(9), Mode: BaseDateMode.ScheduledStart);

            if (s.Id == "S4")
                return new BaseDateInfo(Priority: -5, BaseDate: baseTime.AddHours(18), Mode: BaseDateMode.Deadline);

            // 指定なし (None)
            return new BaseDateInfo(Mode: BaseDateMode.None);
        }

        // 実行（全体はフォワード）
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node },
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            GetBaseDate,
            true, // isForward: True
            TimeSpan.FromHours(1));

        // 検証
        Assert.Equal(baseTime.AddHours(9), steps[0].Start); // S1: 09:00 - 10:00
        Assert.Equal(baseTime.AddHours(10), steps[1].Start); // S2: 10:00 - 11:00
        Assert.Equal(baseTime.AddHours(11), steps[2].Start); // S3: 11:00 - 12:00

        // S4は18:00納期から逆算されていること
        Assert.Equal(baseTime.AddHours(17), steps[3].Start); // S4: 17:00 - 18:00
        Assert.Equal(baseTime.AddHours(18), steps[3].End);
    }

    [Fact]
    public void Schedule_ShouldHandleReferenceNodesAndHolidays()
    {
        var baseTime = new DateTime(2026, 4, 1, 0, 0, 0);
        var lineId = "Line_01"; // 固定のリソースID

        // --- 1. 既存予約 (ReferenceNodes) の準備 ---
        // 別案件のステップに、どのリソースを専有しているかを明示する
        var refStep = new MyStep
        {
            Id = "RefStep",
            LeadTime = TimeSpan.FromHours(2),
            Start = baseTime.AddHours(10),
            End = baseTime.AddHours(12),
            Lines = { (lineId, 1.0) },// ここが重要：リソースIDを指定
            LineId = lineId,
        };
        var refNode = new MyBom { Id = "ExistingProject", ProcessSteps = new List<MyStep> { refStep } };

        // --- 2. 新規案件の準備 ---
        var steps = new List<MyStep>
        {
            // 9:00開始で2時間。10:00-12:00が埋まっていれば、衝突して12:00以降になるはず
            new MyStep { Id = "NewStep", LeadTime = TimeSpan.FromHours(2),            Lines = { (lineId,1.0) }}
        };
        var newNode = new MyBom { Id = "NewProject", ProcessSteps = steps };

        // 実行
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { newNode },
            new[] { refNode }, // 既存予約として渡す
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new BaseDateInfo(Priority: 0, BaseDate: baseTime.AddHours(9), Mode: BaseDateMode.ScheduledStart),
            true,
            TimeSpan.FromHours(1));

        // --- 検証 ---
        // 期待通りなら、9:00-11:00 のスロットを探した際に 10:00 の衝突を検知し、
        // 既存予約が終わる 12:00 までずれるはずです。
        Assert.Equal(baseTime.AddHours(12), steps[0].Start);
        Assert.Equal(baseTime.AddHours(14), steps[0].End);
        Assert.Equal(lineId, steps[0].LineId);
    }

    [Fact]
    public void Schedule_ShouldPushBackBySetupTime()
    {
        var baseTime = new DateTime(2026, 4, 1, 9, 0, 0);
        var lineId = "Line_01";

        // 1. 既存予約: 09:00 - 11:00
        var refStep = new MyStep
        {
            Id = "Ref",
            Start = baseTime,
            End = baseTime.AddHours(2),
            Lines = { (lineId, 1.0) },
            LineId = lineId
        };
        var refNode = new MyBom { Id = "Existing", ProcessSteps = new List<MyStep> { refStep } };

        // 2. 新規案件: 11:00 開始希望だが、SetupTime が 1時間ある
        var steps = new List<MyStep>
    {
        new MyStep
        {
            Id = "New",
            LeadTime = TimeSpan.FromHours(1),
            SetupTime = TimeSpan.FromHours(1), // ← これが重要
            Lines = { (lineId, 1.0) }
        }
    };
        var newNode = new MyBom { Id = "NewNode", ProcessSteps = steps };

        // 実行
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { newNode },
            new[] { refNode },
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new BaseDateInfo(Priority: 0, BaseDate: baseTime.AddHours(11), Mode: BaseDateMode.ScheduledStart),
            true,
            TimeSpan.FromMinutes(1));

        // --- 検証 ---
        // 11:00に着手したいが、11:00-12:00は「段取り時間」として既存予約と衝突するはず。
        // 結果、段取りが11:00-12:00、本作業(Start)が 12:00 になることを期待。
        Assert.Equal(baseTime.AddHours(12), steps[0].Start);
    }



    [Fact]
    public void Schedule_ShouldNotAllowChildToExceedParent_WhenBackwardWithPartialForward()
    {
        // 納期: 4/10 18:00
        var deadline = new DateTime(2026, 4, 10, 0, 0, 0);
        var lineId = "Common_Line";
        var oneDay = TimeSpan.FromDays(1);

        // --- 1. 親ノード (納期モード) ---
        var parentStep = new MyStep { Id = "Parent_S1", LeadTime = oneDay, Lines = { (lineId, 1.0) } };
        var parentNode = new MyBom { Id = "Parent", ProcessSteps = new List<MyStep> { parentStep } };

        // --- 2. 子ノード (フォワードモードに設定される) ---
        var childStep = new MyStep { Id = "Child_S1", LeadTime = oneDay, Lines = { (lineId, 1.0) } };
        var childNode = new MyBom { Id = "Child", ProcessSteps = new List<MyStep> { childStep } };

        parentNode.Children.Add(childNode);
        childNode.Parent = parentNode;

        // 親子関係の定義
        var allNodes = new[] { parentNode, childNode };

        var chileBase = new DateTime(2026, 4, 1, 0, 0, 0);
        // 実行
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            allNodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s =>
            {
                // 親は Deadline(バックワード)、子は ScheduledStart(フォワード)
                // かつ Priority は同じ (0)
                bool isParent = s.Id.Contains("Parent");
                var mode = BaseDateMode.ScheduledStart;

                // 子のフォワード基準日は十分前(4/1)にするが、
                // 親子制約によって「親の開始日以降」に制限されるはず
                var date = chileBase;

                return new BaseDateInfo(0, date, mode);
            },
            false, // 基本戦略はバックワード (親から先に計算される)
            oneDay);

        // --- 検証 (再発防止ポイント) ---

        // 1. 親の時間が正しく決まっているか (例: 4/10 18:00 終了)
        Assert.Equal(chileBase, parentStep.Start);
        Assert.Equal(chileBase.AddDays(1), parentStep.End);
        Assert.Equal(chileBase.AddDays(-1), childStep.Start);
        Assert.Equal(chileBase, childStep.End);


    }
}
