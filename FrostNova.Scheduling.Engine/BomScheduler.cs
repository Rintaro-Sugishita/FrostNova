using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FrostNova.Scheduling.Engine;

public static class BomScheduler
{
    public static void Schedule<TBom, TBomKey, TOperation, TOperationKey>
(
        IEnumerable<TBom> targetNodes,
        IEnumerable<TBom> referenceNodes,
        IEnumerable<IPendingTimeRange> nonWorkingTimes,
        Func<ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
        Func<TOperation, BaseDateInfo> baseDateGetter,
        bool isForward,
        TimeSpan unit)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var resourceManager = new ResourceScheduler(unit, resourceNonWorkingGetter);

        // 既存予約（referenceNodes）の登録
        // targetNodesに含まれるものは再計算対象なので除外
        var targetIds = new HashSet<TBomKey>(targetNodes.Select(n => n.Id));
        foreach (var refNode in referenceNodes.Where(n => !targetIds.Contains(n.Id)))
        {
            foreach (var step in refNode.ProcessSteps)
                resourceManager.Book<TOperation, TOperationKey>(step, step.Start, step.End, step.LineId, step.MemberId); // 既存リソースIDが取れるなら渡す
        }

        // 共通の非稼働時間を登録    
        resourceManager.RegisterGlobalNonWorkingTime(nonWorkingTimes);

        // 全工程をフラットに展開し、計算順序を決定
        // Forward: Levelが大きい（深い/材料）順 -> StepOrder順
        // Backward: Levelが小さい（浅い/製品）順 -> StepOrder逆順
        var allStepsWithMeta = targetNodes
            .SelectMany(node => node.ProcessSteps.Select(step =>
            {
                var a = baseDateGetter(step);

                return new
                {
                    Node = node,
                    Step = step,
                    Info = a,
                };
            }))
            .OrderBy(x => x.Info.Priority)
            .ThenBy(x => isForward ? -x.Node.Level : x.Node.Level)
            .ThenBy(x => isForward ? x.Step.StepOrder : -x.Step.StepOrder)
            .ThenBy(x => x.Node.Priority)
            .ToList();


        var stepCursors = allStepsWithMeta.ToDictionary(
            item => item.Step,
            item => baseDateGetter(item.Step).BaseDate
        );

        var completedSteps = new HashSet<TOperation>();

        foreach (var item in allStepsWithMeta)
        {
            var baseLeadTime = item.Step.LeadTime ?? TimeSpan.Zero;
            var currentStep = item.Step;
            var currentCursor = stepCursors[item.Step];

            var info = item.Info;
            var propagatedConstraint = stepCursors[item.Step];
            // ステップ固有の指定がある場合は、それを優先/マージする
            if (info.Mode != BaseDateMode.None && info.BaseDate != default)
            {
                // ここで前述の Max/Min 比較を行う
                // (例: 納期指定なら Min(伝播値, 指定値) など)
                currentCursor = ApplyStrictConstraint(currentCursor, info);
            }

            bool useForward;
            if (info.Mode == BaseDateMode.None)
            {
                // 指定がない場合は全体の戦略(isForward)に従う
                useForward = isForward;
            }
            else
            {
                // 指定がある場合は Mode に従う (ScheduledStart なら順算、Deadline なら逆算)
                useForward = info.Mode == BaseDateMode.ScheduledStart;
            }

            DateTime? limit = null;
            if (useForward)
            {
                // 【順算の場合の壁 = 自分の後ろにある確定済みの工程の開始日】
                // 同一ノード内の後続工程
                var nextStep = item.Node.ProcessSteps
                    .Where(s => s.StepOrder > currentStep.StepOrder && completedSteps.Contains(s))
                    .OrderBy(s => s.StepOrder)
                    .FirstOrDefault();

                // 親ノード（後続）
                var parentStart = (item.Node.Parent as TBom)?.ProcessSteps
                    .Where(s => completedSteps.Contains(s))
                    .Select(s => s.Start)
                    .Min();

                // これら確定済みの「後ろ」の工程の最小開始日が limit となる
                var constraintDates = new List<DateTime>();
                if (nextStep != null && nextStep.Start.HasValue) constraintDates.Add(nextStep.Start.Value);
                if (parentStart.HasValue) constraintDates.Add(parentStart.Value);

                if (constraintDates.Count > 0) limit = constraintDates.Min();
            }
            else
            {
                // 【逆算の場合の壁 = 自分の前にある確定済みの工程の終了日】
                // 同一ノード内の先行工程
                var prevStep = item.Node.ProcessSteps
                    .Where(s => s.StepOrder < currentStep.StepOrder && completedSteps.Contains(s))
                    .OrderByDescending(s => s.StepOrder)
                    .FirstOrDefault();

                // 子ノード（先行）
                var childEnd = item.Node.Children
                    .SelectMany(c => c.ProcessSteps)
                    .Where(s => completedSteps.Contains(s))
                    .Select(s => s.End)
                    .Max();

                var constraintDates = new List<DateTime>();
                if (prevStep != null && prevStep.End.HasValue) constraintDates.Add(prevStep.End.Value);
                if (childEnd.HasValue) constraintDates.Add(childEnd.Value);

                if (constraintDates.Count > 0) limit = constraintDates.Max();
            }

            // 計算実行
            var result = useForward
                    ? resourceManager.FindBestSlotForward<TOperation, TOperationKey>(item.Step, currentCursor, baseLeadTime, limit)
                    : resourceManager.FindBestSlotBackward<TOperation, TOperationKey>(item.Step, currentCursor, baseLeadTime,limit);

            // 結果を保存
            item.Step.Start = result.Start;
            item.Step.End = result.End;
            item.Step.WorkingSpan = result.WorkingSpan;
            item.Step.LineId = result.LineId;
            item.Step.MemberId = result.MemberId;

            foreach (var other in item.Node.ProcessSteps)
            {
                if (object.ReferenceEquals(other, item.Step)) continue;

                if (other.StepOrder < item.Step.StepOrder)
                {
                    // 自分より前の工程に対して：納期(Deadline)を絞り込む
                    // 既存の制約と、今回の開始日のうち「早い方(Min)」をとる
                    if (result.Start < stepCursors[other]) stepCursors[other] = result.Start;
                }
                else
                {
                    // 自分より後の工程に対して：着手可能日を押し上げる
                    // 既存の制約と、今回の終了日のうち「遅い方(Max)」をとる
                    if (result.End > stepCursors[other]) stepCursors[other] = result.End;
                }
            }

            // --- 計算結果を他の全工程へ伝播させる ---
            PropagateConstraints<TBom, TBomKey, TOperation, TOperationKey>(item.Node, item.Step, isForward, stepCursors);

            // リソース予約の確定
            resourceManager.Book<TOperation, TOperationKey>(item.Step, item.Step.Start, item.Step.End, result.LineId, result.MemberId);

            completedSteps.Add(currentStep);
        }
    }

    /// <summary>
    /// 計算済みのステップから、関連するすべてのステップへ時間的制約を波及させる
    /// </summary>
    private static void PropagateConstraints<TBom, TBomKey, TOperation, TOperationKey>(
        TBom currentNode,
        TOperation calculatedStep,
        bool isForward,
        Dictionary<TOperation, DateTime> stepConstraints)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull

    {
        var start = calculatedStep.Start ?? DateTime.MinValue;
        var end = calculatedStep.End ?? DateTime.MaxValue;

        // 1. 同一ノード内の他のステップへの伝播
        foreach (var other in currentNode.ProcessSteps)
        {
            if (object.ReferenceEquals(other, calculatedStep)) continue;

            if (other.StepOrder < calculatedStep.StepOrder)
            {
                // 先行工程に対して：今回の開始日以前に終わらなければならない (Min)
                if (start < stepConstraints[other]) stepConstraints[other] = start;
            }
            else if (other.StepOrder > calculatedStep.StepOrder)
            {
                // 後続工程に対して：今回の終了日以降に始めなければならない (Max)
                if (end > stepConstraints[other]) stepConstraints[other] = end;
            }
        }

        // 2. 親ノード（後続ノード）への伝播
        // Forward/Backwardに関わらず、親子関係は「子→親」の順序が不変
        if (currentNode.Parent is TBom parentNode)
        {
            foreach (var pStep in parentNode.ProcessSteps)
            {
                // 親（後続）の全ステップは、このノードの最終終了日より後でなければならない
                // ここでは簡易的に「現在のステップの終了日」で押し上げる
                if (end > stepConstraints[pStep]) stepConstraints[pStep] = end;
            }
        }

        // 3. 子ノード（先行ノード）への伝播
        foreach (var childNode in currentNode.Children)
        {
            foreach (var cStep in childNode.ProcessSteps)
            {
                // 子（先行）の全ステップは、このノードの開始日より前に終わらなければならない
                if (start < stepConstraints[cStep]) stepConstraints[cStep] = start;
            }
        }
    }

    /// <summary>
    /// 伝播された基準日と、ユーザー指定の基準日を比較し、より厳しい方の制約を返します。
    /// </summary>
    private static DateTime ApplyStrictConstraint(DateTime propagatedDate, BaseDateInfo info)
    {
        // ユーザー指定がない場合は、伝播された日付をそのまま使う
        if (info.Mode == BaseDateMode.None || info.BaseDate == default)
        {
            return propagatedDate;
        }

        return info.Mode switch
        {
            BaseDateMode.ScheduledStart => info.BaseDate > propagatedDate ? info.BaseDate : propagatedDate,// 開始指定の場合：「この日以降にしか始められない」
                                                                                                           // 伝播された日付と指定日のうち、より「遅い(未来)」方を採用する
            BaseDateMode.Deadline => info.BaseDate < propagatedDate ? info.BaseDate : propagatedDate,// 納期指定の場合：「この日までに終わらせなければならない」
                                                                                                     // 伝播された日付と指定日のうち、より「早い(過去)」方を採用する
            _ => propagatedDate,
        };
    }


    public readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        public readonly ResourceType Type;
        public readonly object Id;

        public ResourceKey(ResourceType type, object id)
        {
            Type = type;
            Id = id;
        }

        public bool Equals(ResourceKey other) => Type == other.Type && Equals(Id, other.Id);
        public override bool Equals(object? obj) => obj is ResourceKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Id);
    }


    public class ResourceKeyComparer : IEqualityComparer<ResourceKey>
    {
        private readonly Dictionary<ResourceType, IEqualityComparer<object>> _comparers;
        private readonly IEqualityComparer<object> _defaultComparer = EqualityComparer<object>.Default;

        public ResourceKeyComparer(Dictionary<ResourceType, IEqualityComparer<object>>? comparers)
        {
            // 外部から渡された比較器の辞書を保持（null なら空の辞書）
            _comparers = comparers ?? new Dictionary<ResourceType, IEqualityComparer<object>>();
        }

        public bool Equals(ResourceKey x, ResourceKey y)
        {
            if (ReferenceEquals(x, y)) return true;

            // 1. まずリソースの種類が違うなら別物
            if (x.Type != y.Type) return false;

            // 2. その種類専用の比較器を取得（なければデフォルト）
            var comparer = GetComparer(x.Type);

            // 3. ID を比較
            return comparer.Equals(x.Id, y.Id);
        }

        public int GetHashCode(ResourceKey obj)
        {

            var comparer = GetComparer(obj.Type);

            unchecked
            {
                int hash = 17;
                hash = hash * 23 + obj.Type.GetHashCode();
                hash = hash * 23 + (obj.Id == null ? 0 : comparer.GetHashCode(obj.Id));
                return hash;
            }
        }

        /// <summary>
        /// 指定された型に対応する比較器を返し、未設定ならデフォルトを返す
        /// </summary>
        private IEqualityComparer<object> GetComparer(ResourceType type)
        {
            return _comparers.TryGetValue(type, out var comparer) ? comparer : _defaultComparer;
        }
    }

    public enum ResourceType { Line, Member, Global }


    //製造ラインとメンバーの指定がある場合、その空き状況を管理しておくクラス

    public class ResourceScheduler
    {
        private readonly List<IPendingTimeRange> _globalNonWorking = new();
        const double DefaultEfficiency = 1.0;

        // キーを object から ResourceKey に変更
        private readonly Dictionary<ResourceKey, List<IPendingTimeRange>> _resourceUsage = new();
        private readonly Func<ResourceKey, IEnumerable<IPendingTimeRange>> _resNonWorkingGetter;
        private readonly TimeSpan _unit;

        public ResourceScheduler(
         TimeSpan unit,
         Func<ResourceKey, IEnumerable<IPendingTimeRange>> resNonWorkingGetter,
         Dictionary<ResourceType, IEqualityComparer<object>>? comparers = null)
        {
            _unit = unit;
            _resNonWorkingGetter = resNonWorkingGetter;
            var keyComparer = new ResourceKeyComparer(comparers);
            _resourceUsage = new Dictionary<ResourceKey, List<IPendingTimeRange>>(keyComparer);
        }
        /// <summary>
        /// Registers global non-working time intervals.
        /// </summary>
        public void RegisterGlobalNonWorkingTime(IPendingTimeRange range) => _globalNonWorking.Add(range);
        public void RegisterGlobalNonWorkingTime(IEnumerable< IPendingTimeRange> ranges) => _globalNonWorking.AddRange(ranges);


        /// <summary>
        /// リソースを予約する。LeadTimeがnullの場合は予約（占有）を行わない。
        /// </summary>
        public void Book<TOperation, TOperationKey>(TOperation step, DateTime? start, DateTime? end, object? lineId, object? memberId)
                    where TOperation : IProcessStep<TOperationKey>
        {
            if (step.LeadTime == null || !start.HasValue || !end.HasValue) return;

            var range = new TimeRangeInstance { Start = start, End = end };
            foreach (var key in GetSelectedResourceKeys(lineId, memberId))
            {
                if (!_resourceUsage.TryGetValue(key, out var usages))
                {
                    usages = new List<IPendingTimeRange>();
                    _resourceUsage[key] = usages;
                }
                usages.Add(range);
            }
        }

        public ScheduleResult FindBestSlotForward<TOperation, TOperationKey>(TOperation step, DateTime startCursor, TimeSpan baseDuration, DateTime? latestFinish = null) where TOperation : IProcessStep<TOperationKey>

        {
            // 効率のデフォルト値 (1.0)
            const double DefaultEfficiency = 1.0;

            // リソース候補を取得（空の場合はデフォルト効率のnullリソースとして扱う）
            var lineCandidates = GetLineCandidates<TOperation, TOperationKey>(step).DefaultIfEmpty((null, DefaultEfficiency));
            var memberCandidates = GetMemberCandidates<TOperation, TOperationKey>(step).DefaultIfEmpty((null, DefaultEfficiency));

            ScheduleResult? bestResult = null;

            foreach (var line in lineCandidates)
                foreach (var member in memberCandidates)
                {
                    // ラインとメンバー、両方の効率を掛け合わせる（例：ライン2.0倍 × 人1.0倍 = 2.0倍速）
                    var totalEfficiency = line.Efficiency * member.Efficiency;

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, totalEfficiency);

                    if (step.LeadTime == null)
                    //return new ScheduleResult(startCursor, startCursor.Add(actualDuration), null, null, actualDuration);
                    {
                        var keys2 = GetSelectedResourceKeys(line.Id, member.Id).ToList();
                        // 順算なら、指定時間以降で最初の稼働時間を探す
                        var adjustedStart = FindFirstAvailableStart(keys2, startCursor, _unit, 0, latestFinish);

                        return new ScheduleResult(adjustedStart.Start, adjustedStart.End, null, null, _unit);
                    }

                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();
                    var searchStart = startCursor.Add(step.SetupTime);

                    var (start, end) = FindFirstAvailableStart(keys, searchStart, actualDuration, step.BatchCapacity, latestFinish);

                    // 「最も早く終わる」組み合わせを保持
                    if (bestResult == null || end < bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(start, end, line.Id, member.Id, actualDuration);
                    }
                }

            // 候補がない場合のフォールバック（効率1.0で計算）
            return bestResult ?? new ScheduleResult(startCursor, startCursor.Add(CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, 1.0)), null, null, baseDuration);
        }
        public ScheduleResult FindBestSlotBackward<TOperation, TOperationKey>(TOperation step, DateTime endCursor, TimeSpan baseDuration, DateTime? earliestStart = null) where TOperation : IProcessStep<TOperationKey>
        {
            const double DefaultEfficiency = 1.0;

            var lineCandidates = GetLineCandidates<TOperation, TOperationKey>(step).DefaultIfEmpty((null, DefaultEfficiency));
            var memberCandidates = GetMemberCandidates<TOperation, TOperationKey>(step).DefaultIfEmpty((null, DefaultEfficiency));

            ScheduleResult? bestResult = null;

            foreach (var line in lineCandidates)
                foreach (var member in memberCandidates)
                {
                    // 効率の掛け合わせ
                    var totalEfficiency = line.Efficiency * member.Efficiency;

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, totalEfficiency);

                    // LeadTimeがnullなら、リソース判定をスキップ
                    if (step.LeadTime == null)
                    //   return new ScheduleResult(endCursor.Subtract(actualDuration), endCursor, null, null, actualDuration);
                    {
                        var keys2 = GetSelectedResourceKeys(line.Id, member.Id).ToList();
                        // 逆算なら、指定時間以前で最初の稼働可能（終了）時間を探す
                        var adjustedEnd = FindFirstAvailableEnd(keys2, endCursor, _unit, 0, earliestStart);

                        return new ScheduleResult(adjustedEnd.Start, adjustedEnd.End, null, null, _unit);
                    }

                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();

                    // バックワードでの探索：指定された完了期限(endCursor)から遡る
                    var (start, end) = FindFirstAvailableEnd(keys, endCursor, actualDuration, step.BatchCapacity, earliestStart);

                    // 判定：完了日が「より納期に近い（＝遅い）」ものをベストとする
                    if (bestResult == null || end > bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(start, end, line.Id, member.Id, actualDuration);
                    }
                }

            // 候補がない場合のフォールバック
            if (bestResult == null)
            {
                var fallbackDuration = CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, DefaultEfficiency);
                return new ScheduleResult(endCursor.Subtract(fallbackDuration), endCursor, null, null, fallbackDuration);
            }

            return bestResult.Value;
        }



        private (DateTime Start, DateTime End) FindFirstAvailableStart(List<ResourceKey> keys, DateTime start, TimeSpan duration, double capacity, DateTime? latestFinish)
        {
            // 1. まず開始地点を稼働時間（非稼働じゃない時間）まで進める
            var currentStart = MoveToAvailableSlot(keys, start, true);

            while (true)
            {
                // 2. 稼働時間だけをカウントして終了候補日を計算
                var endCandidate = AddWorkingTime(keys, currentStart, duration);

                if (latestFinish.HasValue && endCandidate > latestFinish.Value)
                {
                    // 本来はエラーを投げるか、最小限の期間を確保して親の開始日に合わせる
                    // 今回は整合性を優先し、親の開始日に強制クリップする（または例外）
                    return (latestFinish.Value.Subtract(duration), latestFinish.Value);
                }
                // 3. 衝突を確認
                var collision = FindCollisionWithBatch(keys, currentStart, endCandidate, capacity);

                if (collision == null || IsCalendarHoliday(keys, collision))
                {
                    // 衝突がない、またはカレンダーの休みの場合は、この開始・終了ペアが正解
                    return (currentStart, endCandidate);
                }

                // 衝突したのが「カレンダー（休日や昼休み）」である場合、
                // AddWorkingTimeがその時間をすでに「作業時間外」として飛ばして計算しているため、
                // 開始地点(currentStart)をずらす必要はありません。
                // 「他人の予約（Usage）」とぶつかっている場合のみ、開始地点をずらします。
                currentStart = RoundUp(collision.End ?? currentStart.Add(_unit), _unit);
                currentStart = MoveToAvailableSlot(keys, currentStart, true);

            }
        }

        /// <summary>
        /// 指定された期間内で、最も遅い完了時間を探索する（バックワードコアロジック）
        /// </summary>
        private (DateTime Start, DateTime End) FindFirstAvailableEnd(List<ResourceKey> keys, DateTime end, TimeSpan duration, double capacity, DateTime? earliestStart)
        {
            var currentEnd = MoveToAvailableSlot(keys, end, false);
            while (true)
            {
                var startCandidate = SubtractWorkingTime(keys, currentEnd, duration);

                if (earliestStart.HasValue && startCandidate < earliestStart.Value)
                {
                    // 本来はエラーを投げるか、最小限の期間を確保して親の開始日に合わせる
                    // 今回は整合性を優先し、親の開始日に強制クリップする（または例外）
                    return (earliestStart.Value.Subtract(duration), earliestStart.Value);
                }
                var collision = FindCollisionWithBatch(keys, startCandidate, currentEnd, capacity);

                if (collision == null || IsCalendarHoliday(keys, collision))
                {
                    // 衝突がない、またはカレンダー由来の休みの場合は、このペアを返す
                    return (startCandidate, currentEnd);
                }
                // 衝突があった場合は、衝突の開始点（Start）からさらに前（過去）を探す
                currentEnd = RoundDown(collision.Start ?? currentEnd.Subtract(_unit), _unit);
                currentEnd = MoveToAvailableSlot(keys, currentEnd, false);
            }
        }


        /// <summary>
        /// Subtracts working duration by skipping non-working periods.
        /// </summary>
        private DateTime SubtractWorkingTime(IEnumerable<ResourceKey> resources, DateTime end, TimeSpan duration)
        {
            var current = end;
            var remaining = duration;

            while (remaining > TimeSpan.Zero)
            {
                var prev = current.Subtract(_unit);
                if (FindCollision(resources, prev, current) == null)
                {
                    remaining -= _unit;
                }
                current = prev;
            }
            return current;
        }


        private IPendingTimeRange? FindCollision(IEnumerable<ResourceKey> resourceKeys, DateTime start, DateTime end)
        {
            // 1. 全体非稼働との衝突
            var globalHit = _globalNonWorking.FirstOrDefault(nw => IsOverlap(nw, start, end));
            if (globalHit != null) return globalHit;

            foreach (var key in resourceKeys)
            {
                // 2. リソース固有の非稼働（カレンダー）との衝突

                var calendarHit = _resNonWorkingGetter?.Invoke(key).FirstOrDefault(nw => IsOverlap(nw, start, end));

                if (calendarHit != null) return calendarHit;

                // 3. 他の工程予約との衝突
                if (_resourceUsage.TryGetValue(key, out var usages))
                {
                    var bookingHit = usages.FirstOrDefault(u => IsOverlap(u, start, end));
                    if (bookingHit != null) return bookingHit;
                }
            }
            return null;
        }

        private class TimeRangeInstance : IPendingTimeRange { public DateTime? Start { get; set; } public DateTime? End { get; set; } public TimeSpan? WorkingSpan { get; set; } }

        /// <summary>
        /// 見つかった衝突が「カレンダー由来」か「他人の予約由来」かを判定する補助関数
        /// </summary>
        private bool IsCalendarHoliday(List<ResourceKey> keys, IPendingTimeRange collision)
        {
            // グローバル非稼働に含まれているか確認
            if (_globalNonWorking.Any(nw => nw == collision)) return true;

            // 各リソースのカレンダー非稼働に含まれているか確認
            foreach (var key in keys)
            {
                var resNonWorking = _resNonWorkingGetter?.Invoke(key);
                if (resNonWorking != null && resNonWorking.Any(nw => nw == collision))
                {
                    return true;
                }
            }

            // どちらにもなければ、それは _usage（他人の予約）
            return false;
        }
        private TimeSpan CalculateActualDuration<TOperation, TOperationKey>(TOperation step, TimeSpan baseDuration, double efficiency) where TOperation : IProcessStep<TOperationKey>
        {
            // 効率 2.0 = 時間が 1/2 になる
            var effectiveTicks = (long)(baseDuration.Ticks / Math.Max(0.01, efficiency));
            var effectiveBase = TimeSpan.FromTicks(effectiveTicks);

            if (step.IsFixedTime) return effectiveBase;

            var batchCount = Math.Ceiling(step.Quantity / Math.Max(1, step.BatchCapacity));
            return TimeSpan.FromTicks(effectiveBase.Ticks * (long)batchCount);
        }


        private IPendingTimeRange? FindCollisionWithBatch(IEnumerable<ResourceKey> keys, DateTime start, DateTime end, double capacity)
        {
            var globalHit = _globalNonWorking.FirstOrDefault(nw => IsOverlap(nw, start, end));
            if (globalHit != null) return globalHit;

            foreach (var key in keys)
            {

                var calHit = _resNonWorkingGetter?.Invoke(key).FirstOrDefault(nw => IsOverlap(nw, start, end));
                if (calHit != null) return calHit;

                if (_resourceUsage.TryGetValue(key, out var usages))
                {
                    // バッチ考慮：指定時間内に既にキャパを超えて予約があるか
                    var overlaps = usages.Where(u => IsOverlap(u, start, end)).ToList();
                    if (overlaps.Count == 0) return null;
                    if (capacity <= 1 && overlaps.Any()) return overlaps[0];
                }
            }
            return null;
        }

        private DateTime AddWorkingTime(IEnumerable<ResourceKey> keys, DateTime start, TimeSpan duration)
        {
            var current = start;
            var remaining = duration;

            while (remaining > TimeSpan.Zero)
            {
                // FindCollisionWithBatch を「他人の予約(Usage)を無視」して呼び出し、純粋にカレンダーの休みをチェック
                // ※第4引数の capacity に double.MaxValue を渡すことで他人の予約との衝突を無視する
                var collision = FindCollisionWithBatch(keys, current, current.Add(_unit), double.MaxValue);

                if (collision == null)
                {
                    // 稼働時間なので、残り時間を減らして時間を進める
                    remaining -= _unit;
                    current = current.Add(_unit);
                }
                else
                {
                    // 非稼働時間（昼休み等）なので、残り時間は減らさずに、非稼働時間の終わりまでジャンプする
                    current = RoundUp(collision.End ?? current.Add(_unit), _unit);
                }
            }
            return current;
        }

        private DateTime MoveToAvailableSlot(IEnumerable<ResourceKey> keys, DateTime cursor, bool forward)
        {
            while (true)
            {
                //var next = forward ? cursor.Add(_unit) : cursor.Subtract(_unit);
                //var collision = FindCollisionWithBatch(keys, forward ? cursor : next, forward ? next : cursor, double.MaxValue);
                //if (collision == null) return cursor;
                //cursor = forward ? RoundUp(collision.End ?? cursor.Add(_unit), _unit) : RoundDown(collision.Start ?? cursor.Subtract(_unit), _unit);

                var start = forward ? cursor : cursor.Subtract(_unit);
                var end = forward ? cursor.Add(_unit) : cursor;

                // capacity に double.MaxValue を渡しているので、純粋な「非稼働（カレンダー休日）」のみを拾う想定
                var collision = FindCollisionWithBatch(keys, start, end, double.MaxValue);

                // 2. 衝突がない（＝稼働している）なら、この cursor 地点を採用
                if (collision == null) return cursor;

                // 3. 衝突（休日）があるなら、その休日を飛び越えて再試行
                cursor = forward
                    ? RoundUp(collision.End ?? cursor.Add(_unit), _unit)
                    : RoundDown(collision.Start ?? cursor.Subtract(_unit), _unit);
            }
        }

        private IEnumerable<ResourceKey> GetSelectedResourceKeys(object? lId, object? mId)
        {
            if (lId != null) yield return new ResourceKey(ResourceType.Line, lId);
            if (mId != null) yield return new ResourceKey(ResourceType.Member, mId);
        }




        private IEnumerable<(object? Id, double Efficiency)> GetLineCandidates<TOperation, TOperationKey>(TOperation step) where TOperation : IProcessStep<TOperationKey>
            => (step as IResourceConstraint)?.GetLineCandidates() ?? Enumerable.Empty<(object?, double)>();

        private IEnumerable<(object? Id, double Efficiency)> GetMemberCandidates<TOperation, TOperationKey>(TOperation step) where TOperation : IProcessStep<TOperationKey>
            => (step as IResourceConstraint)?.GetMemberCandidates() ?? Enumerable.Empty<(object?, double)>();
        private static bool IsOverlap(IPendingTimeRange r, DateTime s, DateTime e) => r.Start < e && s < r.End;
        private static DateTime RoundUp(DateTime dt, TimeSpan d) => new((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        private static DateTime RoundDown(DateTime dt, TimeSpan d) => new(dt.Ticks / d.Ticks * d.Ticks, dt.Kind);


        public struct ScheduleResult
        {
            public DateTime Start;
            public DateTime End;
            public object? LineId;
            public object? MemberId;
            public TimeSpan WorkingSpan;

            public ScheduleResult(DateTime s, DateTime e, object? l, object? m, TimeSpan workingSpan) { Start = s; End = e; LineId = l; MemberId = m; WorkingSpan = workingSpan; }
        }
    }
}


