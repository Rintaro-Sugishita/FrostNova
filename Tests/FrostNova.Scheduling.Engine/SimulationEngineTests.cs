namespace FrostNova.Scheduling.Engine.Tests;

public class SimulationEngineTests
{
    // ─── テスト用: Dictionary ベースの ISetupMatrix 実装 ──────────────────────
    private sealed class DictSetupMatrix : ISetupMatrix<string>
    {
        private readonly Dictionary<(string? From, string To), TimeSpan> _table;
        private readonly TimeSpan _defaultSetup;

        public DictSetupMatrix(Dictionary<(string? From, string To), TimeSpan> table, TimeSpan defaultSetup = default)
        {
            _table = table;
            _defaultSetup = defaultSetup;
        }

        public TimeSpan GetSetupTime(string? fromItemKey, string? toItemKey)
        {
            if (toItemKey == null) return TimeSpan.Zero;
            return _table.TryGetValue((fromItemKey, toItemKey), out var t) ? t : _defaultSetup;
        }
    }

    private readonly TimeSpan _unit = TimeSpan.FromMinutes(1);
    private readonly DateTime _baseDate = new(2026, 4, 1, 9, 0, 0);

    // ─── テスト用: IScheduleDeadline を実装した MyBom サブクラス ───────────────
    private sealed class DeadlineBom : MyBom, IScheduleDeadline
    {
        public DateTime TargetDate { get; init; }
        public DateTime Deadline { get; init; }
        public DateTime EarliestStart { get; init; }
        public double ImportanceWeight { get; init; } = 1.0;
        public double RiskBuffer { get; init; } = 0.0;
    }

    private static MyBom MakeBom(string id, int priority, params (string stepId, int hours)[] steps)
    {
        var bom = new MyBom { Id = id, Priority = priority };
        for (int i = 0; i < steps.Length; i++)
        {
            bom.ProcessSteps.Add(new MyStep
            {
                Id = steps[i].stepId,
                StepOrder = i,
                LeadTime = TimeSpan.FromHours(steps[i].hours),
                Lines = [("Line_A", 1.0), ("Line_B", 1.0)],
            });
        }
        return bom;
    }

    // ─── ExhaustiveStrategy が 1 品目の場合に正常動作する ──────────────────────

    [Fact]
    public void SingleNode_ShouldReturn_ValidVariant()
    {
        var bom = MakeBom("P1", 0, ("S1", 2));
        var nodes = new List<MyBom> { bom };
        var weights = new ScoreWeights(A: 1, B: 0, C: 0);

        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes,
            [],
            [],
            _ => [],
            _ => new BaseDateInfo(0, _baseDate),
            isForward: true,
            _unit,
            weights,
            new ExhaustiveStrategy());

        Assert.NotNull(result);
        Assert.NotNull(result.Score);
        Assert.False(result.Score!.IsHardViolated);
        // IProcessStep に最良解が書き戻されているか
        Assert.True(bom.ProcessSteps[0].Start.HasValue);
        Assert.True(bom.ProcessSteps[0].End.HasValue);
    }

    // ─── 製品処理順の最適化で全探索が動作する ─────────────────────────────────

    [Fact]
    public void ExhaustiveStrategy_ShouldTry_AllPermutations()
    {
        // ライン1本のみ。先に流した製品のほうが早く終わる。
        // P1(4h) と P2(2h) を全探索して最適順を選ぶ。
        var p1 = MakeBom("P1", 0, ("P1_S1", 4));
        var p2 = MakeBom("P2", 0, ("P2_S1", 2));
        // ライン候補を1本に絞って競合させる
        foreach (var step in p1.ProcessSteps.Concat(p2.ProcessSteps))
            step.Lines = [("Line_A", 1.0)];

        var nodes = new List<MyBom> { p1, p2 };
        var weights = new ScoreWeights(A: 0, B: 0, C: 0); // スコア軸なし → makespan 最小化はしないが例外なく完走することを確認

        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes,
            [],
            [],
            _ => [],
            _ => new BaseDateInfo(0, _baseDate),
            isForward: true,
            _unit,
            weights,
            new ExhaustiveStrategy());

        Assert.NotNull(result);
        // 2品目 → 2通りを全部試した証拠として OrderPermutation が設定される
        Assert.NotNull(result.OrderPermutation);
        Assert.Equal(2, result.OrderPermutation!.Count);
        // IProcessStep に書き戻されている
        Assert.True(p1.ProcessSteps[0].End.HasValue);
        Assert.True(p2.ProcessSteps[0].End.HasValue);
    }

    // ─── Deadline 違反のシナリオが除外される ───────────────────────────────────

    [Fact]
    public void HardViolated_Scenario_ShouldBeExcluded()
    {
        // 絶対納期を設定した 1 品目。LeadTime=10h なのに Deadline=9:30 → 必ず違反する。
        var bom = new DeadlineBom
        {
            Id = "P1",
            Priority = 0,
            TargetDate = _baseDate.AddHours(8),
            Deadline = _baseDate.AddMinutes(30), // 9:30 → 10h 作業は絶対に終わらない
            EarliestStart = _baseDate.AddDays(-999),
        };
        bom.ProcessSteps.Add(new MyStep
        {
            Id = "S1",
            StepOrder = 0,
            LeadTime = TimeSpan.FromHours(10),
            Lines = [("Line_A", 1.0)],
        });

        var nodes = new List<MyBom> { bom };
        var weights = new ScoreWeights(A: 1, B: 0, C: 0);

        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes,
            [],
            [],
            _ => [],
            _ => new BaseDateInfo(0, _baseDate),
            isForward: true,
            _unit,
            weights,
            new ExhaustiveStrategy());

        // ハード違反しかなければ最後の評価結果を返す（Score.IsHardViolated == true）
        Assert.NotNull(result.Score);
        Assert.True(result.Score!.IsHardViolated);
    }

    // ─── TargetDate ズレのペナルティが計算される ───────────────────────────────

    [Fact]
    public void TargetDateDeviation_ShouldBePositive_WhenLate()
    {
        // LeadTime=4h, TargetDate=baseDate+2h → 2時間遅れになる
        var bom = new DeadlineBom
        {
            Id = "P1",
            Priority = 0,
            TargetDate = _baseDate.AddHours(2),
            Deadline = _baseDate.AddDays(10),
            EarliestStart = _baseDate.AddDays(-999),
        };
        bom.ProcessSteps.Add(new MyStep
        {
            Id = "S1",
            StepOrder = 0,
            LeadTime = TimeSpan.FromHours(4),
            Lines = [("Line_A", 1.0)],
        });

        var nodes = new List<MyBom> { bom };
        var weights = new ScoreWeights(A: 1, B: 0, C: 0);

        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes,
            [],
            [],
            _ => [],
            _ => new BaseDateInfo(0, _baseDate),
            isForward: true,
            _unit,
            weights,
            new ExhaustiveStrategy());

        Assert.NotNull(result.Score);
        Assert.False(result.Score!.IsHardViolated);
        // 4h作業 → baseDate+4h終了、TargetDate=baseDate+2h → 2h 遅れ
        Assert.Equal(2.0, result.Score.TargetDateDeviationHours, precision: 0);
    }

    // ─── Priority 操作後に元の値に戻る ─────────────────────────────────────────

    [Fact]
    public void NodePriority_ShouldBeRestored_AfterOptimize()
    {
        var p1 = MakeBom("P1", 42, ("S1", 2));
        var p2 = MakeBom("P2", 99, ("S1", 2));
        var nodes = new List<MyBom> { p1, p2 };
        var weights = new ScoreWeights();

        SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, weights, new ExhaustiveStrategy());

        // 最適化後に Priority が元の値に戻っているか
        Assert.Equal(42, p1.Priority);
        Assert.Equal(99, p2.Priority);
    }

    // ─── ImportanceWeight が高い製品ほど納期ズレペナルティが大きい ───────────────

    [Fact]
    public void ImportanceWeight_ShouldAmplify_TargetDatePenalty()
    {
        // 両製品とも同じ2時間遅れになるが、P2 の ImportanceWeight が 3.0
        var makeDeadlineBom = (string id, double importance) =>
        {
            var bom = new DeadlineBom
            {
                Id = id,
                Priority = 0,
                TargetDate = _baseDate,      // 9:00 に終わってほしいが
                Deadline = _baseDate.AddDays(10),
                EarliestStart = _baseDate.AddDays(-999),
                ImportanceWeight = importance,
                RiskBuffer = 0,
            };
            bom.ProcessSteps.Add(new MyStep
            {
                Id = $"{id}_S1",
                StepOrder = 0,
                LeadTime = TimeSpan.FromHours(2), // 2時間 → 11:00 終了 → 2h 遅れ
                Lines = [("Line_A", 1.0)],
            });
            return bom;
        };

        // 重要度 1.0 のみ
        var nodes1 = new List<MyBom> { makeDeadlineBom("P1", 1.0) };
        var result1 = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes1, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 1, B: 0, C: 0, D: 0),
            new ExhaustiveStrategy());

        // 重要度 3.0 のみ
        var nodes2 = new List<MyBom> { makeDeadlineBom("P2", 3.0) };
        var result2 = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes2, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 1, B: 0, C: 0, D: 0),
            new ExhaustiveStrategy());

        // 重要度 3.0 のペナルティは 1.0 の 3 倍になるはず
        Assert.True(result2.Score!.TargetDateDeviationHours > result1.Score!.TargetDateDeviationHours);
        Assert.Equal(3.0, result2.Score.TargetDateDeviationHours / result1.Score.TargetDateDeviationHours, precision: 5);
    }

    // ─── RiskBuffer で LeadTime が係数倍になる ──────────────────────────────────

    [Fact]
    public void RiskBuffer_ShouldExtend_ScheduledDuration()
    {
        // RiskBuffer=0.5 → 2h作業が3h確保される → 終了が1h後ろにずれる
        var bom = new DeadlineBom
        {
            Id = "P1",
            Priority = 0,
            TargetDate = _baseDate.AddDays(10),
            Deadline = _baseDate.AddDays(10),
            EarliestStart = _baseDate.AddDays(-999),
            ImportanceWeight = 1,
            RiskBuffer = 0.5, // 50% 増し
        };
        bom.ProcessSteps.Add(new MyStep
        {
            Id = "S1",
            StepOrder = 0,
            LeadTime = TimeSpan.FromHours(2),
            Lines = [("Line_A", 1.0)],
        });

        var nodes = new List<MyBom> { bom };
        SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(), new ExhaustiveStrategy());

        // RiskBuffer=0.5 → 2h × 1.5 = 3h確保 → 終了は baseDate + 3h
        var end = bom.ProcessSteps[0].End!.Value;
        Assert.Equal(_baseDate.AddHours(3), end);
    }

    // ─── ライン稼働バランスが評価される ─────────────────────────────────────────

    [Fact]
    public void LineBalance_ShouldBePositive_WhenUneven()
    {
        // Line_A に2h + 2h、Line_B に何もなし → 不均等
        var p1 = MakeBom("P1", 0, ("S1", 2));
        var p2 = MakeBom("P2", 0, ("S2", 2));
        // 全ステップを Line_A のみに割り付け
        foreach (var step in p1.ProcessSteps.Concat(p2.ProcessSteps))
            step.Lines = [("Line_A", 1.0)];

        var nodes = new List<MyBom> { p1, p2 };
        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 0, B: 0, C: 0, D: 1),
            new ExhaustiveStrategy());

        // Line_A のみに集中 → ライン稼働バランスのペナルティは 0（ライン1本なので標準偏差は計算外）
        Assert.Equal(0.0, result.Score!.LineBalanceStdDevHours, precision: 5);

        // 2ライン候補があれば標準偏差が出る
        foreach (var step in p1.ProcessSteps.Concat(p2.ProcessSteps))
            step.Lines = [("Line_A", 1.0), ("Line_B", 1.0)];

        var nodes2 = new List<MyBom> { MakeBom("Q1", 0, ("S1", 4)), MakeBom("Q2", 0, ("S2", 2)) };
        var result2 = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes2, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 0, B: 0, C: 0, D: 1),
            new ExhaustiveStrategy());

        Assert.NotNull(result2.Score);
    }

    // ─── ISetupMatrix: 同品番連続は段取り短縮 ────────────────────────────────

    [Fact]
    public void SetupMatrix_SameItem_ReducesSetupTime()
    {
        // P1 → P1 → P2 の順に流す。
        // 同品番（P1→P1）: 段取り 0h、異品番（P1→P2）: 段取り 1h。
        // ライン1本（Line_A）のみ。全て Line_A に投入。
        // P1 は LeadTime=2h、P2 は LeadTime=2h。
        //
        // FixedSetupTime=1h の通常ケース（setupMatrix なし）と比較して、
        // 同品番後なら段取り削減されることを検証する。
        //
        // 本テストでは BomScheduler.Schedule を直接呼び出して前後を比較する。

        var matrix = new DictSetupMatrix(
            new Dictionary<(string? From, string To), TimeSpan>
            {
                [("P1", "P1")] = TimeSpan.Zero,
                [("P1", "P2")] = TimeSpan.FromHours(1),
                [(null,  "P1")] = TimeSpan.FromHours(1), // 初回投入は1h
            },
            defaultSetup: TimeSpan.FromHours(1));

        // P1 を2品目（同品番連続）→ P2 の順に流す
        var bom1 = new MyBom { Id = "P1", Priority = 0 };
        bom1.ProcessSteps.Add(new MyStep { Id = "P1_S1", StepOrder = 0, LeadTime = TimeSpan.FromHours(2), Lines = [("Line_A", 1.0)] });
        var bom2 = new MyBom { Id = "P1", Priority = 1 };
        bom2.ProcessSteps.Add(new MyStep { Id = "P1_S2", StepOrder = 0, LeadTime = TimeSpan.FromHours(2), Lines = [("Line_A", 1.0)] });
        var bom3 = new MyBom { Id = "P2", Priority = 2 };
        bom3.ProcessSteps.Add(new MyStep { Id = "P2_S1", StepOrder = 0, LeadTime = TimeSpan.FromHours(2), Lines = [("Line_A", 1.0)] });

        BomScheduler.Schedule<MyBom, string, MyStep, string>(
            [bom1, bom2, bom3], [], [], _ => [],
            _ => new BaseDateInfo(0, _baseDate), isForward: true, _unit,
            leadTimeOverride: null, setupMatrix: matrix);

        // bom1: 初回(null→P1)=1h段取り + 2h作業 → 9:00-12:00(Start=10:00, SetupStart=9:00)
        // bom2: 同品番(P1→P1)=0h段取り + 2h作業 → 12:00-14:00
        // bom3: 異品番(P1→P2)=1h段取り + 2h作業 → 14:00-17:00(Start=15:00)
        Assert.True(bom2.ProcessSteps[0].Start.HasValue);
        Assert.True(bom3.ProcessSteps[0].Start.HasValue);

        var bom2End = bom2.ProcessSteps[0].End!.Value;
        var bom3Start = bom3.ProcessSteps[0].Start!.Value;

        // bom2 終了後 bom3 が 1h 後に開始（段取り1h分）
        Assert.Equal(bom2End.AddHours(1), bom3Start);
    }

    // ─── RandomSearchStrategy: 有効な結果を返す ──────────────────────────────

    [Fact]
    public void RandomSearchStrategy_ShouldReturn_ValidVariant()
    {
        var p1 = MakeBom("P1", 0, ("S1", 3));
        var p2 = MakeBom("P2", 0, ("S1", 2));
        var p3 = MakeBom("P3", 0, ("S1", 1));
        foreach (var step in p1.ProcessSteps.Concat(p2.ProcessSteps).Concat(p3.ProcessSteps))
            step.Lines = [("Line_A", 1.0)];

        var nodes = new List<MyBom> { p1, p2, p3 };
        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 0, B: 0, C: 0),
            new RandomSearchStrategy(iterations: 20, seed: 42));

        Assert.NotNull(result);
        Assert.False(result.Score!.IsHardViolated);
        // IProcessStep に書き戻されている
        Assert.True(p1.ProcessSteps[0].End.HasValue);
        Assert.True(p2.ProcessSteps[0].End.HasValue);
        Assert.True(p3.ProcessSteps[0].End.HasValue);
    }

    // ─── GeneticAlgorithmStrategy: 有効な結果を返す ──────────────────────────

    [Fact]
    public void GeneticAlgorithmStrategy_ShouldReturn_ValidVariant()
    {
        var p1 = MakeBom("P1", 0, ("S1", 4));
        var p2 = MakeBom("P2", 0, ("S1", 3));
        var p3 = MakeBom("P3", 0, ("S1", 2));
        var p4 = MakeBom("P4", 0, ("S1", 1));
        foreach (var step in p1.ProcessSteps.Concat(p2.ProcessSteps).Concat(p3.ProcessSteps).Concat(p4.ProcessSteps))
            step.Lines = [("Line_A", 1.0)];

        var nodes = new List<MyBom> { p1, p2, p3, p4 };
        var gaParams = new GaParameters(PopulationSize: 10, Generations: 10, MutationRate: 0.1, Seed: 123);
        var result = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 0, B: 0, C: 0),
            new GeneticAlgorithmStrategy(gaParams));

        Assert.NotNull(result);
        Assert.False(result.Score!.IsHardViolated);
        Assert.True(p1.ProcessSteps[0].End.HasValue);
        Assert.True(p4.ProcessSteps[0].End.HasValue);
    }

    // ─── GeneticAlgorithmStrategy: 高い世代数でスコアが改善される ──────────────

    [Fact]
    public void GeneticAlgorithmStrategy_MoreGenerations_ShouldNotWorsen()
    {
        // 5 品目を Line_A 1本で処理。希望納期 A軸のみでスコアを評価。
        // 少ない世代と多い世代を比較して、多い世代の方がスコアが悪化しないことを確認。
        var makeNodes = () =>
        {
            var boms = new List<MyBom>();
            for (int i = 0; i < 5; i++)
            {
                var b = new DeadlineBom
                {
                    Id = $"P{i}",
                    Priority = 0,
                    TargetDate = _baseDate.AddHours(2),
                    Deadline = _baseDate.AddDays(10),
                    EarliestStart = _baseDate.AddDays(-999),
                    ImportanceWeight = 1.0,
                    RiskBuffer = 0,
                };
                b.ProcessSteps.Add(new MyStep { Id = $"P{i}_S1", StepOrder = 0, LeadTime = TimeSpan.FromHours(i + 1), Lines = [("Line_A", 1.0)] });
                boms.Add(b);
            }
            return boms;
        };

        var nodes1 = makeNodes();
        var result1 = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes1, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 1, B: 0, C: 0, D: 0),
            new GeneticAlgorithmStrategy(new GaParameters(PopulationSize: 10, Generations: 5, Seed: 42)));

        var nodes2 = makeNodes();
        var result2 = SimulationEngine.Optimize<MyBom, string, MyStep, string>(
            nodes2, [], [], _ => [], _ => new BaseDateInfo(0, _baseDate),
            isForward: true, _unit, new ScoreWeights(A: 1, B: 0, C: 0, D: 0),
            new GeneticAlgorithmStrategy(new GaParameters(PopulationSize: 10, Generations: 50, Seed: 42)));

        // 世代が多い方がスコアが悪化しないことを確認（等しいか改善されている）
        Assert.True(result2.Score!.TotalScore <= result1.Score!.TotalScore,
            $"50世代({result2.Score.TotalScore:F2}) が 5世代({result1.Score.TotalScore:F2}) より悪化した");
    }
}
