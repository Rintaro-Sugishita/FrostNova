using System.Text;
using Xunit;
using static System.Net.Mime.MediaTypeNames;
namespace FrostNova.Scheduling.Engine.Tests;

public class BomSchedulerBackwardTests
{
    private readonly TimeSpan _unitDay = TimeSpan.FromDays(1);
    private readonly DateTime _deadline = new(2026, 5, 20); // 基準となる納期

    // --- 1. 基本リードタイム・設備なしのテスト ---

    [Fact]
    public void BackwardCalculation_WithoutResources_SimpleLeadTime()
    {
        // リードタイム3日の工程。設備制約なし。
        // 5/20納期の場合、5/17開始、5/20終了になることを確認。
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromDays(3) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        Assert.Equal(_deadline.AddDays(-3), step.Start);
        Assert.Equal(_deadline, step.End);
    }

    [Fact]
    public void Backward_StepOrder_SequentialInNode()
    {
        // 1つのノード内に2つの工程（Step1: 2日, Step2: 1日）。
        // バックワードなので、Step2(後工程)が納期に張り付き、Step1がその前に配置される。
        var s1 = new MyStep { Id = "S1", StepOrder = 1, LeadTime = TimeSpan.FromDays(2) };
        var s2 = new MyStep { Id = "S2", StepOrder = 2, LeadTime = TimeSpan.FromDays(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s1, s2 } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        // 期待値: S2(5/19-5/20), S1(5/17-5/19)
        Assert.Equal(_deadline, s2.End);
        Assert.Equal(s2.Start, s1.End);
        Assert.Equal(_deadline.AddDays(-3), s1.Start);
    }

    // --- 2. 休日（非稼働）のテスト ---

    [Fact]
    public void Backward_SkipsNonWorkingDays()
    {
        // リードタイム2日。納期5/20(水)。
        // 5/19(火)を休日（非稼働）に設定。
        // 期待値: 5/20(水)作業 -> 5/19(火)休み -> 5/18(月)作業。よって開始は5/18。
        var holiday = new MyRange(new(2026, 5, 19), new(2026, 5, 20));
        var step = new MyStep { Id = "S1", LeadTime = TimeSpan.FromDays(2) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), new[] { holiday },
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        Assert.Equal(_deadline.AddDays(-3), step.Start); // 5/18
        Assert.Equal(_deadline.AddDays(-1), step.End);              // 5/20
        // 期間中(5/18-5/20)に1日休日があるので、実質2日分の稼働が確保されている
    }

    // --- 3. BOM構造（親子）のテスト ---

    [Fact]
    public void Backward_ParentChild_Dependency()
    {
        // 親(1日) <- 子(2日) のBOM。納期は親に対して設定。
        // バックワードでは親の開始日が子の完了期限（納期）を押し下げる。
        var childStep = new MyStep { Id = "ChildStep", LeadTime = TimeSpan.FromDays(2) };
        var parentStep = new MyStep { Id = "ParentStep", LeadTime = TimeSpan.FromDays(1) };

        var childNode = new MyBom { Id = "C", Level = 1, ProcessSteps = new List<MyStep> { childStep } };
        var parentNode = new MyBom { Id = "P", Level = 0, ProcessSteps = new List<MyStep> { parentStep } };

        childNode.Parent = parentNode;
        parentNode.Children.Add(childNode);

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { childNode, parentNode }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), // 親の納期
            false, _unitDay);

        // 期待値: 
        // 親: 5/19 - 5/20 (1日間)
        // 子: 5/17 - 5/19 (2日間、親の開始5/19に完了を合わせる)
        Assert.Equal(_deadline, parentStep.End);
        Assert.Equal(parentStep.Start, childStep.End);
        Assert.Equal(_deadline.AddDays(-3), childStep.Start);
    }

    // --- 4. バッチ・数量・効率のテスト ---
    //OK
    [Fact]
    public void Backward_BatchQuantity_DayUnit()
    {
        // リードタイム1日。数量25個。バッチキャパ10個。
        // 25 / 10 = 2.5 -> 3バッチ分必要。
        // 期待値: 1日 × 3 = 3日間の所要。
        var step = new MyStep
        {
            Id = "BatchStep",
            LeadTime = TimeSpan.FromDays(1),
            Quantity = 25,
            BatchCapacity = 10,
            IsFixedTime = false
        };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        Assert.Equal(TimeSpan.FromDays(3), step.End - step.Start);
    }

    [Fact]
    public void Backward_ShouldFillCoverage()
    {
        // 全体をバックワード（isForward: false）にする
        var baseTime = new DateTime(2026, 4, 1, 18, 0, 0); // 18時納期
        var lineId = "Line_01";

        var steps = new List<MyStep>
    {
        new MyStep
        {
            Id = "BackStep",
            LeadTime = TimeSpan.FromHours(2),
            Lines = { (lineId, 1.0) }
        }
    };
        var node = new MyBom { Id = "BackNode", ProcessSteps = steps };

        // 実行
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node },
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new BaseDateInfo(Priority: 0, BaseDate: baseTime, Mode: BaseDateMode.Deadline), // 納期指定
            false, // isForward: false (これでバックワードロジックへ流す)
            TimeSpan.FromMinutes(1));

        // 検証
        Assert.Equal(baseTime.AddHours(-2), steps[0].Start);
        Assert.Equal(baseTime, steps[0].End);
    }

    [Fact]
    public void Backward_SetupTime_Pushback()
    {
        // リードタイム2日、段取り(SetupTime)1日。
        // 期待値: 作業に2日、その前に段取り1日が必要。合計3日前から開始。
        // 注意: 現在の実装では SetupTime は作業期間に含まれない「着手前の空き」として計算
        var step = new MyStep { Id = "SetupStep", LeadTime = TimeSpan.FromDays(2), SetupTime = TimeSpan.FromDays(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { step } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        // バックワードの場合、FindFirstAvailableEndが「段取り分」を考慮して
        // 他の予約とぶつからない最も遅い時間を探す。
        Assert.Equal(_deadline, step.End);
        Assert.Equal(_deadline.AddDays(-2), step.Start);
        // Step内部のStart/Endは純粋な作業時間。SetupTimeは「その前に必要だった時間」
    }

    [Fact]
    public void Backward_LeadTimeNull_OverlapAllowed()
    {
        // LeadTimeがnullの工程。リソースを占有しない。
        // 2つの工程を同じ日に計算してもぶつからないことを確認。
        var s1 = new MyStep { Id = "S1", LeadTime = null, Quantity = 1 }; // nullなので即時
        var s2 = new MyStep { Id = "S2", LeadTime = TimeSpan.FromDays(1) };
        var node = new MyBom { Id = "N1", ProcessSteps = new List<MyStep> { s1, s2 } };

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { node }, Enumerable.Empty<MyBom>(), Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => new(0, _deadline), false, _unitDay);

        // S1はLeadTime nullなので、計算上は「Start == End == 納期」になる
        Assert.Equal(_deadline.Add(-_unitDay), s1.Start);
        Assert.Equal(_deadline, s1.End);
    }
    [Fact]
    public void Backward_ShouldHandleMixedBaseDateModes()
    {
        // --- 条件 ---
        // 全体納期: 4/1 18:00
        // 工程構成: 
        //   Step1 (1h) -> Step2 (2h, 開始指定!) -> Step3 (1h) -> Step4 (1h)
        // 
        // Step2 の開始指定: 4/1 10:00 (ここが起点になる)
        // 
        // --- 期待される動き (バックワード順で計算) ---
        // 1. Step4: 18:00納期から逆算 -> 17:00-18:00
        // 2. Step3: 17:00納期から逆算 -> 16:00-17:00
        // 3. Step2: [ScheduledStart 10:00] 指定 -> 10:00から未来へ配置 -> 10:00-12:00
        // 4. Step1: Step2の開始(10:00)を納期として逆算 -> 09:00-10:00
        //
        // 結果: 9:00(S1開始) ～ 18:00(S4終了) の間に全ての工程が論理的に並ぶこと

        var baseTime = new DateTime(2026, 4, 1, 0, 0, 0);
        var steps = new List<MyStep>
    {
        new MyStep { Id = "S1",StepOrder = 0, LeadTime = TimeSpan.FromHours(1) },
        new MyStep { Id = "S2",StepOrder = 1, LeadTime = TimeSpan.FromHours(2) }, // ここがキー
        new MyStep { Id = "S3",StepOrder = 2, LeadTime = TimeSpan.FromHours(1) },
        new MyStep { Id = "S4",StepOrder = 3, LeadTime = TimeSpan.FromHours(1) }
    };
        var node = new MyBom { Id = "MixedNode", ProcessSteps = steps };

        // baseDateGetter の定義
        BaseDateInfo GetBaseDate(MyStep s)
        {
            if (s.Id == "S2")
                return new(-10, baseTime.AddHours(10), BaseDateMode.ScheduledStart); // S2は10時着手
            if (s.Id == "S4")
                return new(0, baseTime.AddHours(18));       // S4は18時納期

            return new(0, baseTime.AddHours(18));
        }
        var allNodes = new[] { node };
        // 実行（全体はバックワード）
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            allNodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            GetBaseDate,
            false, // Backward
            TimeSpan.FromHours(1));

        // 検証
        Assert.Equal(baseTime.AddHours(9), steps[0].Start); // S1: 09:00 - 10:00
        Assert.Equal(baseTime.AddHours(10), steps[1].Start); // S2: 10:00 - 12:00 (Start指定通り)
        Assert.Equal(baseTime.AddHours(12), steps[1].End);
        Assert.Equal(baseTime.AddHours(16), steps[2].Start); // S3: 16:00 - 17:00 (S4からの逆算結果)
        Assert.Equal(baseTime.AddHours(17), steps[3].Start); // S4: 17:00 - 18:00 (納期通り)
    }


    [Fact]
    public void Schedule_Backward_ShouldResolveResourceConflict_ByDate()
    {
        // 基準となる納期：4/10 18:00
        var deadline = new DateTime(2026, 4, 10, 18, 0, 0);
        var lineId = "CommonLine_01";

        // --- 1. ノードA (優先度高: -10) ---
        // 4/10 18:00から逆算して、4/10中に配置されるはず
        var stepA = new MyStep
        {
            Id = "StepA",
            LeadTime = TimeSpan.FromHours(8), // 1日分(8h)の作業
            Lines = { (lineId, 1.0) }
        };
        var nodeA = new MyBom { Id = "NodeA", ProcessSteps = new List<MyStep> { stepA } };

        // --- 2. ノードB (優先度低: 0) ---
        // Aが4/10を占有するため、Bは4/9に押し出されるはず
        var stepB = new MyStep
        {
            Id = "StepB",
            LeadTime = TimeSpan.FromHours(8),
            Lines = { (lineId, 1.0) }
        };
        var nodeB = new MyBom { Id = "NodeB", ProcessSteps = new List<MyStep> { stepB } };

        // 実行（全体戦略をバックワードに設定）
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { nodeA, nodeB },
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => {
                // 両方とも 4/10 18:00 納期
                if (s.Id == "StepA")
                    return new BaseDateInfo(Priority: -10, BaseDate: deadline, Mode: BaseDateMode.Deadline);
                else
                    return new BaseDateInfo(Priority: 0, BaseDate: deadline, Mode: BaseDateMode.Deadline);
            },
            false, // isForward: false (バックワード実行)
            TimeSpan.FromHours(1)); // 1時間単位の精度

        // --- 検証 ---

        // NodeA: 4/10 10:00 - 18:00 (納期の直前に配置)
        Assert.Equal(deadline, stepA.End);
        Assert.Equal(deadline.AddHours(-8), stepA.Start);

        // NodeB: 4/10はAが使っているので、その手前の空き(4/9)まで譲る
        // 期待：4/9 10:00 - 18:00
        Assert.Equal(new DateTime(2026, 4, 10, 10, 0, 0), stepB.End);
        Assert.Equal(new DateTime(2026, 4, 10, 2, 0, 0), stepB.Start);
    }

    [Fact]
    public void Schedule_Backward_ShouldResolveResourceConflict_ByDate2()
    {
        // 基準となる納期：4/10 18:00
        var deadline = new DateTime(2026, 4, 10, 0, 0, 0);
        var lineId = "CommonLine_01";

        // --- 1. ノードA (優先度高: -10) ---
        // 4/10 18:00から逆算して、4/10中に配置されるはず
        var stepA = new MyStep
        {
            Id = "StepA",
            LeadTime = TimeSpan.FromDays(1), // 1日分(8h)の作業
            Lines = { (lineId, 1.0) }
        };
        var nodeA = new MyBom { Id = "NodeA", ProcessSteps = new List<MyStep> { stepA } };

        // --- 2. ノードB (優先度低: 0) ---
        // Aが4/10を占有するため、Bは4/9に押し出されるはず
        var stepB = new MyStep
        {
            Id = "StepB",
            LeadTime = TimeSpan.FromDays(1),
            Lines = { (lineId, 1.0) }
        };
        var nodeB = new MyBom { Id = "NodeB", ProcessSteps = new List<MyStep> { stepB } };

        // 実行（全体戦略をバックワードに設定）
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            new[] { nodeA, nodeB },
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => {
                // 両方とも 4/10 18:00 納期
                if (s.Id == "StepA") return new BaseDateInfo(Priority: -10, BaseDate: deadline, Mode: BaseDateMode.Deadline);
                return new BaseDateInfo(Priority: 0, BaseDate: deadline, Mode: BaseDateMode.Deadline);
            },
            false, // isForward: false (バックワード実行)
            TimeSpan.FromDays(1)); // 1時間単位の精度

        // --- 検証 ---

        // NodeA: 4/10 10:00 - 18:00 (納期の直前に配置)
        Assert.Equal(deadline, stepA.End);
        Assert.Equal(deadline.AddDays(-1), stepA.Start);

        // NodeB: 4/10はAが使っているので、その手前の空き(4/9)まで譲る
        // 期待：4/9 10:00 - 18:00
        Assert.Equal(deadline.AddDays(-1), stepB.End);
        Assert.Equal(deadline.AddDays(-2), stepB.Start);
    }


    [Fact]
    public void Schedule_ComplexBOM_Daily_HybridConflict_Test()
    {
        // 納期: 4/10 00:00
        var deadline = new DateTime(2026, 4, 10, 0, 0, 0);
        var lineId = "Common_Line";
        var oneDay = TimeSpan.FromDays(1);

        // --- 案件1 (優先度高: -10) ---
        var item1_Child = CreateNode("I1_C", "I1_C1", "I1_C2", lineId);
        var item1_Parent = CreateNode("I1_P", "I1_P1", null, lineId);
        item1_Parent.Children .Add( item1_Child);
        item1_Child.Parent = item1_Parent;

        // --- 案件2 (優先度低: 0) ---
        var item2_Child = CreateNode("I2_C", "I2_C1", "I2_C2", lineId);
        var item2_Parent = CreateNode("I2_P", "I2_P1", null, lineId);
        item2_Parent.Children.Add(item2_Child);
        item2_Child.Parent = item2_Parent;

        var allNodes = new[] { item1_Parent, item1_Child, item2_Parent, item2_Child };

        // 実行 (精度を 1日に設定)
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            allNodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => {
                // C1は「4/6以降に着手」のフォワード。それ以外は 4/10納期。
                bool isC1 = s.Id.EndsWith("C1");
                var mode = isC1 ? BaseDateMode.ScheduledStart : BaseDateMode.Deadline;
                var date = isC1 ? new DateTime(2026, 4, 6, 0, 0, 0) : deadline;
                var priority = s.Id.StartsWith("I1") ? -10 : 0;

                return new BaseDateInfo(priority, date, mode);
            },
            false, // 全体はバックワード
            oneDay); // ★計算精度を 1日に設定

        // --- 検証 ---

        // 案件1（優先）の配置予想:
        // I1_C1(Fwd): 4/6 09:00 - 4/7 09:00
        // I1_C2(Bwd): 4/9 18:00 - 4/10 18:00 (親の締切から逆算)
        // I1_P1(Bwd): 4/10 18:00 - 4/11 18:00 (※親子制約による押し出し)

        var i1_c1 = item1_Child.ProcessSteps[0];
        var i2_c1 = item2_Child.ProcessSteps[0];

        // 同じ 4/6 09:00 着手希望だが、案件1が優先されるため、案件2は 4/7 にズレているか？
        Assert.Equal(new DateTime(2026, 4, 6, 0, 0, 0), i1_c1.Start);
        Assert.Equal(new DateTime(2026, 4, 7, 0, 0, 0), i2_c1.Start);

        // すべての工程で Start < End が維持されているか（逆転エラーのチェック）
        foreach (var node in allNodes)
        {
            foreach (var step in node.ProcessSteps)
            {
                Assert.True(step.Start < step.End, $"工程 {step.Id} で時間逆転: {step.Start} >= {step.End}");
            }
        }
    }
    [Fact]
    public void Schedule_ComplexBOM_Daily_HybridConflict_Test2()
    {
        // 納期: 4/10 00:00
        var deadline = new DateTime(2026, 4, 10, 0, 0, 0);
        var lineId = "Common_Line";
        var oneDay = TimeSpan.FromDays(1);

        // --- 案件1 (優先度高: -10) ---
        var item1_Parent = new MyBom()
        {
            Id = "I1_P",
            ProcessSteps = [
                new (){Id="I1_P1", LeadTime = TimeSpan.FromDays(1), StepOrder = 1, Lines = {(lineId, 1.0) } },
                new (){Id="I1_P2", LeadTime = TimeSpan.FromDays(1), StepOrder = 2 },
                ]
        };
        var item1_Child = new MyBom()
        {
            Id = "I1_C",
            Level = 1,
            ProcessSteps = [
                new (){Id="I1_C1", LeadTime = null, StepOrder = 1 },
                ]
        };
        item1_Parent.Children .Add( item1_Child);
        item1_Child.Parent = item1_Parent;

        // --- 案件2 (優先度低: 0) ---
        var item2_Parent = new MyBom()
        {
            Id = "I2_P",
            ProcessSteps = [
                new (){Id="I2_P1", LeadTime = TimeSpan.FromDays(1), StepOrder = 1, Lines = {(lineId, 1.0) } },
                new (){Id="I2_P2", LeadTime = TimeSpan.FromDays(1), StepOrder = 2 },
                ]
        };
        var item2_Child = new MyBom()
        {
            Id = "I2_C",
            Level = 1,
            ProcessSteps = [
                new (){Id="I2_C1", LeadTime = null , StepOrder = 1},
                ]
        };

        // --- 案件3 (優先度低: 0) ---
        var item3_Parent = new MyBom()
        {
            Id = "I3_P",
            ProcessSteps = [
                new (){Id="I3_P1", LeadTime = TimeSpan.FromDays(1), StepOrder = 1, Lines = {(lineId, 1.0) } },
                new (){Id="I3_P2", LeadTime = TimeSpan.FromDays(1), StepOrder = 2 },
                ]
        };
        var item3_Child = new MyBom()
        {
            Id = "I3_C",
            Level = 1,
            ProcessSteps = [
                new (){Id="I3_C1", LeadTime = null , StepOrder = 1},
                ]
        };

        item2_Parent.Children.Add(item2_Child);
        item2_Child.Parent = item2_Parent;

        var allNodes = new[] { item1_Parent, item1_Child, item2_Parent, item2_Child, item3_Parent, item3_Child };

        // 実行 (精度を 1日に設定)
        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            allNodes,
            Enumerable.Empty<MyBom>(),
            Enumerable.Empty<IPendingTimeRange>(),
            k => Enumerable.Empty<IPendingTimeRange>(),
            s => {
                // C1は「4/6以降に着手」のフォワード。それ以外は 4/10納期。
                bool isC1 = s.Id.EndsWith("P1");
                var mode = isC1 ? BaseDateMode.ScheduledStart : BaseDateMode.Deadline;
                var date = isC1 ? new DateTime(2026, 4, 6, 0, 0, 0) : deadline;
                var priority = s.Id.EndsWith("P1") ? -10 : 0;

                return new BaseDateInfo(priority, date, mode);
            },
            false, // 全体はバックワード
            oneDay); // ★計算精度を 1日に設定

        // --- 検証 ---

        // 案件1（優先）の配置予想:
        // I1_C1(Fwd): 4/6 09:00 - 4/7 09:00
        // I1_C2(Bwd): 4/9 18:00 - 4/10 18:00 (親の締切から逆算)
        // I1_P1(Bwd): 4/10 18:00 - 4/11 18:00 (※親子制約による押し出し)

        var i1_c1 = item1_Parent.ProcessSteps[0];
        var i2_c1 = item2_Parent.ProcessSteps[0];
        var i3_c1 = item3_Parent.ProcessSteps[0];

        // 同じ 4/6 09:00 着手希望だが、案件1が優先されるため、案件2は 4/7 にズレているか？
        Assert.Equal(new DateTime(2026, 4, 6, 0, 0, 0), i1_c1.Start);
        Assert.Equal(new DateTime(2026, 4, 7, 0, 0, 0), i2_c1.Start);
        Assert.Equal(new DateTime(2026, 4, 8, 0, 0, 0), i3_c1.Start);

        // すべての工程で Start < End が維持されているか（逆転エラーのチェック）
        foreach (var node in allNodes)
        {
            foreach (var step in node.ProcessSteps)
            {
                Assert.True(step.Start < step.End, $"工程 {step.Id} で時間逆転: {step.Start} >= {step.End}");
            }
        }
    }
    // ヘルパーメソッド
    private MyBom CreateNode(string nodeId, string s1Id, string? s2Id, string lineId)
    {
        var steps = new List<MyStep> {
            new MyStep { Id = s1Id, LeadTime = TimeSpan.FromDays(1), Lines = { (lineId, 1.0) } }
        };
        if (s2Id != null)
            steps.Add(new MyStep { Id = s2Id, LeadTime = TimeSpan.FromDays(1) });

        return new MyBom { Id = nodeId, ProcessSteps = steps };
    }



}



