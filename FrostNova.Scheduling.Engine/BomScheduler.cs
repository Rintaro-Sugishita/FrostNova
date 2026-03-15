using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FrostNova.Scheduling.Engine;

public static class BomScheduler<TBom, TBomKey, TOperation, TOperationKey>
    where TBom : IBomNode<TBom,TBomKey, TOperationKey, TOperation>
    where TOperation : IProcessStep<TOperationKey>
    where TBomKey : notnull
{
    public static void Schedule(
        IEnumerable<TBom> targetNodes,
        IEnumerable<TBom> referenceNodes,
        IEnumerable<IPendingTimeRange> nonWorkingTimes,
        Func<ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
        Func<TOperation, (int priority, DateTime standardDay)> standardDayGetter,
        bool isForward,
        TimeSpan unit)
    {
        var resourceManager = new ResourceScheduler(unit,resourceNonWorkingGetter);

        // 既存予約（referenceNodes）の登録
        // targetNodesに含まれるものは再計算対象なので除外
        var targetIds = new HashSet<TBomKey>(targetNodes.Select(n => n.Id));
        foreach (var refNode in referenceNodes.Where(n => !targetIds.Contains(n.Id)))
        {
            foreach (var step in refNode.ProcessSteps)
                resourceManager.Book(step, step.Start, step.End, null, null); // 既存リソースIDが取れるなら渡す
        }

        // 共通の非稼働時間を登録
        foreach (var nw in nonWorkingTimes)
        {
            resourceManager.RegisterGlobalNonWorkingTime(nw);
        }

        // 全工程をフラットに展開し、計算順序を決定
        // Forward: Levelが大きい（深い/材料）順 -> StepOrder順
        // Backward: Levelが小さい（浅い/製品）順 -> StepOrder逆順
        var allStepsWithMeta = targetNodes
            .SelectMany(node => node.ProcessSteps.Select(step =>
            {
                var a = standardDayGetter(step);

                return new
                {
                    Node = node,
                    Step = step,
                    ImediatePriority = a.priority,
                    StandardDate = a.standardDay
                };
            }))
            .OrderBy(x=> x.ImediatePriority)
            .ThenBy(x => isForward ? -x.Node.Level : x.Node.Level)
            .ThenBy(x => isForward ? x.Step.StepOrder : -x.Step.StepOrder)
            .ThenBy(x => x.Node.Priority)
            .ToList();

     

        var dynamicStandardDates = targetNodes.ToDictionary(
            n => n.Id,
            n => n.ProcessSteps.Select(s => standardDayGetter(s).standardDay).FirstOrDefault()
        );

        foreach (var item in allStepsWithMeta)
        {
            var baseLeadTime = item.Step.LeadTime ?? TimeSpan.Zero;
            var currentCursor = dynamicStandardDates[item.Node.Id];

            ResourceScheduler.ScheduleResult result;

            if (isForward)
            {
                result = resourceManager.FindBestSlotForward(item.Step, currentCursor, baseLeadTime);
                item.Step.Start = result.Start;
                item.Step.End = result.End;
                item.Step.WorkingSpan = result.WorkingSpan;
                dynamicStandardDates[item.Node.Id] = result.End;
                // 次工程（親ノードなど）の基準日を更新
                UpdateDependentStandardDates(item.Node, item.Step.End.Value, true, dynamicStandardDates);
            }
            else
            {
                result = resourceManager.FindBestSlotBackward(item.Step, currentCursor, baseLeadTime);
                item.Step.Start = result.Start;
                item.Step.End = result.End;
                item.Step.WorkingSpan = result.WorkingSpan;
                dynamicStandardDates[item.Node.Id] = result.Start;
                // 次工程（子ノードなど）の基準日を更新
                UpdateDependentStandardDates(item.Node, item.Step.Start.Value, false, dynamicStandardDates);
            }

            // リソース予約の確定
            resourceManager.Book(item.Step, item.Step.Start, item.Step.End, result.LineId, result.MemberId);
        }
        // 3. 実行ループ
        //foreach (var item in allStepsWithMeta)
        //{
        //    // 現在保持しているそのノードの基準日を取得
        //    var cursor = dynamicStandardDates[item.Node.Id];
        //    var baseTime = item.Step.LeadTime ?? TimeSpan.Zero;
        //    ResourceScheduler.ScheduleResult res;

        //    if (isForward)
        //    {
        //        res = resourceManager.FindBestSlotForward(item.Step, cursor, baseTime);
        //        item.Step.Start = res.Start;
        //        item.Step.End = res.End;

        //        // 【修正ポイント】同一ノード内の次の工程のために基準日を更新
        //        dynamicStandardDates[item.Node.Id] = item.Step.End.Value;

        //        // 親子間の伝播
        //        UpdateDependentStandardDates(item.Node, item.Step.End.Value, true, dynamicStandardDates);
        //    }
        //    else
        //    {
        //        res = resourceManager.FindBestSlotBackward(item.Step, cursor, baseTime);
        //        item.Step.Start = res.Start;
        //        item.Step.End = res.End;

        //        // 【修正ポイント】同一ノード内の前の工程のために基準日を更新
        //        dynamicStandardDates[item.Node.Id] = item.Step.Start.Value;

        //        // 親子間の伝播
        //        UpdateDependentStandardDates(item.Node, item.Step.Start.Value, false, dynamicStandardDates);
        //    }
        //    resourceManager.Book(item.Step, item.Step.Start, item.Step.End, res.LineId, res.MemberId);
        //}
    }


    /// <summary>
    /// 親子間、または工程間の依存関係に基づいて、関連ノードの計算開始/終了基準日を更新します。
    /// </summary>
    private static void UpdateDependentStandardDates(
        TBom currentNode,
        DateTime referenceDate,
        bool isForward,
        Dictionary<TBomKey, DateTime> dateMap)
    {
        if (isForward)
        {
            // 親がいる場合、子の完了日は親の開始可能日を押し上げる(Max)
            if (currentNode.Parent is TBom parentNode && dateMap.ContainsKey(parentNode.Id))
            {
                if (referenceDate > dateMap[parentNode.Id])
                    dateMap[parentNode.Id] = referenceDate;
            }
        }
        else
        {
            // 子がいる場合、親の開始日は子の完了期限を押し下げる(Min)
            foreach (var child in currentNode.Children)
            {
                if (child is TBom childNode && dateMap.ContainsKey(childNode.Id))
                {
                    if (referenceDate < dateMap[childNode.Id])
                        dateMap[childNode.Id] = referenceDate;
                }
            }
        }
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
        private readonly IEqualityComparer<object> _idComparer;

        public ResourceKeyComparer(IEqualityComparer<object>? idComparer = null)
        {
            _idComparer = idComparer ?? EqualityComparer<object>.Default;
        }

        public bool Equals(ResourceKey x, ResourceKey y)
        {
            return x.Type == y.Type && _idComparer.Equals(x.Id, y.Id);
        }

        public int GetHashCode(ResourceKey obj)
        {
            return HashCode.Combine(obj.Type, _idComparer.GetHashCode(obj.Id));
        }
    }

    public enum ResourceType { Line, Member, Global }


    //製造ラインとメンバーの指定がある場合、その空き状況を管理しておくクラス

    internal class ResourceScheduler
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
         IEqualityComparer<object>? idComparer = null)
        {
            _unit = unit;
            _resNonWorkingGetter = resNonWorkingGetter;
            _resourceUsage = new Dictionary<ResourceKey, List<IPendingTimeRange>>(new ResourceKeyComparer(idComparer));
        }
        /// <summary>
        /// Registers global non-working time intervals.
        /// </summary>
        public void RegisterGlobalNonWorkingTime(IPendingTimeRange range) => _globalNonWorking.Add(range);


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
                var resNonWorking = _resNonWorkingGetter(key);
                var calendarHit = resNonWorking.FirstOrDefault(nw => IsOverlap(nw, start, end));
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
        /// リソースを予約する。LeadTimeがnullの場合は予約（占有）を行わない。
        /// </summary>
        public void Book(TOperation step, DateTime? start, DateTime? end, object? lineId, object? memberId)
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

        /// <summary>
        /// 最も早く終了するリソースの組み合わせを探し、結果を返す
        /// </summary>
        //public ScheduleResult FindBestSlotForward(TOperation step, DateTime startCursor, TimeSpan baseDuration)
        //{
        //    var actualDuration = CalculateActualDuration(step, baseDuration);

        //    // LeadTimeがnullなら、リソース判定をスキップして即座に開始日を返す
        //    if (step.LeadTime == null)
        //        return new ScheduleResult(startCursor, startCursor.Add(actualDuration), null, null);

        //    var lineIds = GetLineCandidates(step).DefaultIfEmpty(null);
        //    var memberIds = GetMemberCandidates(step).DefaultIfEmpty(null);

        //    ScheduleResult? bestResult = null;

        //    foreach (var lId in lineIds)
        //        foreach (var mId in memberIds)
        //        {
        //            var keys = GetSelectedResourceKeys(lId, mId).ToList();
        //            // 段取り時間を考慮して探索開始時間をずらす
        //            var searchStart = startCursor.Add(step.SetupTime);
        //            var start = FindFirstAvailableStart(keys, searchStart, actualDuration, step.BatchCapacity);

        //            if (bestResult == null || start < bestResult.Value.Start)
        //            {
        //                bestResult = new ScheduleResult(start, start.Add(actualDuration), lId, mId);
        //            }
        //        }

        //    return bestResult ?? new ScheduleResult(startCursor, startCursor.Add(actualDuration), null, null);
        //}
        public ScheduleResult FindBestSlotForward(TOperation step, DateTime startCursor, TimeSpan baseDuration)
        {
            // 効率のデフォルト値 (1.0)
            const double DefaultEfficiency = 1.0;

            // リソース候補を取得（空の場合はデフォルト効率のnullリソースとして扱う）
            var lineCandidates = GetLineCandidates(step).DefaultIfEmpty((null, DefaultEfficiency));
            var memberCandidates = GetMemberCandidates(step).DefaultIfEmpty((null, DefaultEfficiency));

            ScheduleResult? bestResult = null;

            foreach (var line in lineCandidates)
                foreach (var member in memberCandidates)
                {
                    // ラインとメンバー、両方の効率を掛け合わせる（例：ライン2.0倍 × 人1.0倍 = 2.0倍速）
                    var totalEfficiency = line.Efficiency * member.Efficiency;

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration(step, baseDuration, totalEfficiency);

                    if (step.LeadTime == null)
                        return new ScheduleResult(startCursor, startCursor.Add(actualDuration), null, null, actualDuration);

                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();
                    var searchStart = startCursor.Add(step.SetupTime);

                    var (start, end) = FindFirstAvailableStart(keys, searchStart, actualDuration, step.BatchCapacity);

                    // 「最も早く終わる」組み合わせを保持
                    if (bestResult == null || end < bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(start, end, line.Id, member.Id, actualDuration);
                    }
                }

            // 候補がない場合のフォールバック（効率1.0で計算）
            return bestResult ?? new ScheduleResult(startCursor, startCursor.Add(CalculateActualDuration(step, baseDuration, 1.0)), null, null, baseDuration);
        }

        private TimeSpan CalculateActualDuration(TOperation step, TimeSpan baseDuration, double efficiency)
        {
            // 効率 2.0 = 時間が 1/2 になる
            var effectiveTicks = (long)(baseDuration.Ticks / Math.Max(0.01, efficiency));
            var effectiveBase = TimeSpan.FromTicks(effectiveTicks);

            if (step.IsFixedTime) return effectiveBase;

            var batchCount = Math.Ceiling(step.Quantity / Math.Max(1, step.BatchCapacity));
            return TimeSpan.FromTicks(effectiveBase.Ticks * (long)batchCount);
        }
        // --- 内部計算ロジック ---

        //private DateTime FindFirstAvailableStart(List<ResourceKey> keys, DateTime start, TimeSpan duration, double capacity)
        //{
        //    var currentStart = MoveToAvailableSlot(keys, start, true);
        //    while (true)
        //    {
        //        var endCandidate = AddWorkingTime(keys, currentStart, duration);
        //        var collision = FindCollisionWithBatch(keys, currentStart, endCandidate, capacity);
        //        if (collision == null) return currentStart;

        //        currentStart = RoundUp(collision.End ?? currentStart.Add(_unit), _unit);
        //        currentStart = MoveToAvailableSlot(keys, currentStart, true);
        //    }
        //}
        //private DateTime FindFirstAvailableStart(List<ResourceKey> keys, DateTime start, TimeSpan duration, double capacity)
        //{
        //    // 1. まず開始地点を稼働時間まで進める
        //    var currentStart = MoveToAvailableSlot(keys, start, true);

        //    while (true)
        //    {
        //        // 2. 「カレンダーの休み」を飛ばしながら、実作業時間分を確保できる終了日を計算
        //        var endCandidate = AddWorkingTime(keys, currentStart, duration);

        //        // 3. 【重要】ここでは「リソースの競合（他人の予約）」だけをチェックする
        //        // ※CheckCol内部で、グローバル非稼働やカレンダー非稼働は除外し、
        //        //   _usage（他人の予約）との衝突のみを返すようにするか、
        //        //   あるいはここで「他人の予約」による衝突かどうかを判定します。
        //        var collision = FindCollisionWithBatch(keys, currentStart, endCandidate, capacity);

        //        if (collision == null)
        //            return currentStart;

        //        // 4. 他人の予約とぶつかった場合のみ、開始地点をその予約の後ろにずらす
        //        currentStart = RoundUp(collision.End ?? currentStart.Add(_unit), _unit);
        //        currentStart = MoveToAvailableSlot(keys, currentStart, true);
        //    }
        //}

        private (DateTime Start, DateTime End) FindFirstAvailableStart(List<ResourceKey> keys, DateTime start, TimeSpan duration, double capacity)
        {
            // 1. まず開始地点を稼働時間（非稼働じゃない時間）まで進める
            var currentStart = MoveToAvailableSlot(keys, start, true);

            while (true)
            {
                // 2. 稼働時間だけをカウントして終了候補日を計算
                var endCandidate = AddWorkingTime(keys, currentStart, duration);

                // 3. 衝突を確認
                var collision = FindCollisionWithBatch(keys, currentStart, endCandidate, capacity);

                if (collision == null || IsCalendarHoliday(keys, collision))
                {
                    // 衝突がない、またはカレンダーの休みの場合は、この開始・終了ペアが正解
                    return (currentStart, endCandidate);
                }

                // 【ここが修正のキモ】
                // 衝突したのが「カレンダー（休日や昼休み）」である場合、
                // AddWorkingTimeがその時間をすでに「作業時間外」として飛ばして計算しているため、
                // 開始地点(currentStart)をずらす必要はありません。
                // 「他人の予約（Usage）」とぶつかっている場合のみ、開始地点をずらします。
                currentStart = RoundUp(collision.End ?? currentStart.Add(_unit), _unit);
                currentStart = MoveToAvailableSlot(keys, currentStart, true);

                //bool isCalendarHoliday = IsCalendarHoliday(keys, collision);
                //if (isCalendarHoliday)
                //{
                //    // カレンダーの休みなら、このままこの currentStart で OK
                //    return currentStart;
                //}

                //// 4. 他人の予約とぶつかった場合のみ、開始地点をその予約の後ろにずらして再試行
                //currentStart = RoundUp(collision.End ?? currentStart.Add(_unit), _unit);
                //currentStart = MoveToAvailableSlot(keys, currentStart, true);
            }
        }

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
                if (_resNonWorkingGetter(key).Any(nw => nw == collision)) return true;
            }

            // どちらにもなければ、それは _usage（他人の予約）
            return false;
        }


        /// <summary>
        /// 最も遅く完了可能なリソースの組み合わせを探し、結果を返す（バックワード）
        /// </summary>
        //public ScheduleResult FindBestSlotBackward(TOperation step, DateTime endCursor, TimeSpan baseDuration)
        //{
        //    var actualDuration = CalculateActualDuration(step, baseDuration);

        //    // LeadTimeがnullなら、リソース判定をスキップ
        //    if (step.LeadTime == null)
        //        return new ScheduleResult(endCursor.Subtract(actualDuration), endCursor, null, null);

        //    var lineIds = GetLineCandidates(step).DefaultIfEmpty(null);
        //    var memberIds = GetMemberCandidates(step).DefaultIfEmpty(null);

        //    ScheduleResult? bestResult = null;

        //    foreach (var lId in lineIds)
        //        foreach (var mId in memberIds)
        //        {
        //            var keys = GetSelectedResourceKeys(lId, mId).ToList();

        //            // バックワードでの探索
        //            var end = FindFirstAvailableEnd(keys, endCursor, actualDuration, step.BatchCapacity);
        //            var start = end.Subtract(actualDuration);

        //            // 段取り時間は「着手前」に必要なため、段取りを含めた開始日が他と衝突しないか、
        //            // あるいは段取り分だけさらに後ろ倒しが必要かを考慮する場合はここで調整します。
        //            // 今回はシンプルに「完了日が最も遅い（＝納期に最も近い）もの」をベストとします。
        //            if (bestResult == null || end > bestResult.Value.End)
        //            {
        //                bestResult = new ScheduleResult(start, end, lId, mId);
        //            }
        //        }

        //    return bestResult ?? new ScheduleResult(endCursor.Subtract(actualDuration), endCursor, null, null);
        //}
        public ScheduleResult FindBestSlotBackward(TOperation step, DateTime endCursor, TimeSpan baseDuration)
        {
            const double DefaultEfficiency = 1.0;

            var lineCandidates = GetLineCandidates(step).DefaultIfEmpty((null, DefaultEfficiency));
            var memberCandidates = GetMemberCandidates(step).DefaultIfEmpty((null, DefaultEfficiency));

            ScheduleResult? bestResult = null;

            foreach (var line in lineCandidates)
                foreach (var member in memberCandidates)
                {
                    // 効率の掛け合わせ
                    var totalEfficiency = line.Efficiency * member.Efficiency;

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration(step, baseDuration, totalEfficiency);

                    // LeadTimeがnullなら、リソース判定をスキップ
                    if (step.LeadTime == null)
                        return new ScheduleResult(endCursor.Subtract(actualDuration), endCursor, null, null, actualDuration);

                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();

                    // バックワードでの探索：指定された完了期限(endCursor)から遡る
                    var (start,end) = FindFirstAvailableEnd(keys, endCursor, actualDuration, step.BatchCapacity);

                    // 判定：完了日が「より納期に近い（＝遅い）」ものをベストとする
                    if (bestResult == null || end > bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(start, end, line.Id, member.Id, actualDuration);
                    }
                }

            // 候補がない場合のフォールバック
            if (bestResult == null)
            {
                var fallbackDuration = CalculateActualDuration(step, baseDuration, DefaultEfficiency);
                return new ScheduleResult(endCursor.Subtract(fallbackDuration), endCursor, null, null, fallbackDuration);
            }

            return bestResult.Value;
        }
        /// <summary>
        /// 指定された期間内で、最も遅い完了時間を探索する（バックワードコアロジック）
        /// </summary>
        private (DateTime Start, DateTime End) FindFirstAvailableEnd(List<ResourceKey> keys, DateTime end, TimeSpan duration, double capacity)
        {
            var currentEnd = MoveToAvailableSlot(keys, end, false);
            while (true)
            {
                var startCandidate = SubtractWorkingTime(keys, currentEnd, duration);
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




        private IPendingTimeRange? FindCollisionWithBatch(IEnumerable<ResourceKey> keys, DateTime start, DateTime end, double capacity)
        {
            var globalHit = _globalNonWorking.FirstOrDefault(nw => IsOverlap(nw, start, end));
            if (globalHit != null) return globalHit;

            foreach (var key in keys)
            {
                var calHit = _resNonWorkingGetter(key).FirstOrDefault(nw => IsOverlap(nw, start, end));
                if (calHit != null) return calHit;

                if (_resourceUsage.TryGetValue(key, out var usages))
                {
                    // バッチ考慮：指定時間内に既にキャパを超えて予約があるか
                    var overlaps = usages.Where(u => IsOverlap(u, start, end)).ToList();
                    if (capacity <= 1 && overlaps.Any()) return overlaps[0];
                    if (overlaps.Count >= capacity) return overlaps[0];
                }
            }
            return null;
        }

        private TimeSpan CalculateActualDuration(TOperation step, TimeSpan baseDuration)
        {
            if (step.IsFixedTime) return baseDuration;
            var batchCount = Math.Ceiling(step.Quantity / Math.Max(1, step.BatchCapacity));
            return TimeSpan.FromTicks(baseDuration.Ticks * (long)batchCount);
        }

        private DateTime AddWorkingTime(IEnumerable<ResourceKey> keys, DateTime start, TimeSpan duration)
        {
            //var (current, remaining) = (start, duration);
            //while (remaining > TimeSpan.Zero)
            //{
            //    var next = current.Add(_unit);
            //    if (FindCollisionWithBatch(keys, current, next, double.MaxValue) == null) remaining -= _unit;
            //    current = next;
            //}
            //return current;
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
                var next = forward ? cursor.Add(_unit) : cursor.Subtract(_unit);
                var collision = FindCollisionWithBatch(keys, forward ? cursor : next, forward ? next : cursor, double.MaxValue);
                if (collision == null) return cursor;
                cursor = forward ? RoundUp(collision.End ?? cursor.Add(_unit), _unit) : RoundDown(collision.Start ?? cursor.Subtract(_unit), _unit);
            }
        }

        private IEnumerable<ResourceKey> GetSelectedResourceKeys(object? lId, object? mId)
        {
            if (lId != null) yield return new ResourceKey(ResourceType.Line, lId);
            if (mId != null) yield return new ResourceKey(ResourceType.Member, mId);
        }




        private IEnumerable<(object Id, double Efficiency)> GetLineCandidates(TOperation step)
            => (step as IResourceConstraint)?.GetLineCandidates() ?? Enumerable.Empty<(object, double)>();

        private IEnumerable<(object Id, double Efficiency)> GetMemberCandidates(TOperation step)
            => (step as IResourceConstraint)?.GetMemberCandidates() ?? Enumerable.Empty<(object, double)>();
        //private IEnumerable<object> GetLineCandidates(TOperation step) => (step as IResourceConstraint)?.GetLineCandidates() ?? Enumerable.Empty<object>();
        //private IEnumerable<object> GetMemberCandidates(TOperation step) => (step as IResourceConstraint)?.GetMemberCandidates() ?? Enumerable.Empty<object>();

        private static bool IsOverlap(IPendingTimeRange r, DateTime s, DateTime e) => r.Start < e && s < r.End;
        private static DateTime RoundUp(DateTime dt, TimeSpan d) => new((dt.Ticks + d.Ticks - 1) / d.Ticks * d.Ticks, dt.Kind);
        private static DateTime RoundDown(DateTime dt, TimeSpan d) => new(dt.Ticks / d.Ticks * d.Ticks, dt.Kind);


        public struct ScheduleResult
        {
            public DateTime Start; public DateTime End; public object? LineId; public object? MemberId;

            public TimeSpan WorkingSpan;
            public ScheduleResult(DateTime s, DateTime e, object? l, object? m, TimeSpan workingSpan) { Start = s; End = e; LineId = l; MemberId = m; WorkingSpan = workingSpan; }
        }
    }
}


