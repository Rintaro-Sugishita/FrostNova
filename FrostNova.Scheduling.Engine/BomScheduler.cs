using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FrostNova.Scheduling.Engine;


/// <summary>
/// Provides scheduling logic for bill of materials (BOM) process steps, supporting forward and backward scheduling with
/// resource and non-working time constraints.
/// </summary>
/// <remarks>The BomScheduler class is designed for advanced manufacturing or production planning scenarios where
/// process steps must be scheduled across multiple resources, considering both global and resource-specific non-working
/// periods. It supports both forward (from material to product) and backward (from product to material) scheduling
/// strategies, and can handle complex dependencies between process steps, resources, and BOM hierarchies. All members
/// are static and thread safety is ensured by design. This class is intended for use in applications that require
/// detailed and constraint-aware scheduling, such as MES or APS systems.</remarks>
public static class BomScheduler
{

    /// <summary>
    /// Schedules process steps for the specified target nodes, assigning start and end times based on resource
    /// availability, non-working periods, and scheduling direction.
    /// </summary>
    /// <remarks>This method updates the start and end times of each process step in the target nodes based on
    /// resource constraints, non-working periods, and scheduling direction. It ensures that steps are scheduled in a
    /// valid order, respecting dependencies and existing reservations. The method does not return a value; results are
    /// reflected by updating the properties of the process steps in the provided target nodes.</remarks>
    /// <typeparam name="TBom">The type representing a bill of materials (BOM) node, which must implement IBomNode with the specified key and
    /// operation types.</typeparam>
    /// <typeparam name="TBomKey">The type used as the unique identifier for BOM nodes. Must be non-nullable.</typeparam>
    /// <typeparam name="TOperation">The type representing a process step, which must implement IProcessStep with the specified operation key type.</typeparam>
    /// <typeparam name="TOperationKey">The type used as the unique identifier for process steps.</typeparam>
    /// <param name="targetNodes">The collection of BOM nodes to be scheduled. Each node's process steps will be assigned start and end times.</param>
    /// <param name="referenceNodes">The collection of BOM nodes representing existing reservations. Steps from these nodes are considered as already
    /// scheduled and are excluded from rescheduling if they are also present in targetNodes.</param>
    /// <param name="nonWorkingTimes">A collection of global non-working time ranges that apply to all resources. These periods are excluded from
    /// scheduling.</param>
    /// <param name="resourceNonWorkingGetter">A function that returns non-working time ranges for a given resource key. Used to determine resource-specific
    /// availability.</param>
    /// <param name="baseDateGetter">A function that retrieves base date information for a given process step. Used to determine scheduling
    /// constraints and priorities.</param>
    /// <param name="isForward">A value indicating whether to schedule forward (from earliest possible date) or backward (from latest possible
    /// date).</param>
    /// <param name="unit">The minimum time unit for scheduling operations. Used as the default lead time if not specified for a step.</param>
    public static IReadOnlyList<ScheduleViolationInfo> Schedule<TBom, TBomKey, TOperation, TOperationKey>
(
        IEnumerable<TBom> targetNodes,
        IEnumerable<TBom> referenceNodes,
        IEnumerable<IPendingTimeRange> nonWorkingTimes,
        Func<ResourceKey, IEnumerable<IPendingTimeRange>> resourceNonWorkingGetter,
        Func<TOperation, BaseDateInfo> baseDateGetter,
        bool isForward,
        TimeSpan unit,
        Dictionary<ResourceType, IEqualityComparer<object>>? comparers = null
        )
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull
    {
        var violations = new List<ScheduleViolationInfo>();
        var resourceManager = new ResourceScheduler(unit, resourceNonWorkingGetter, comparers);

        // 既存予約（referenceNodes）の登録
        // targetNodesに含まれるものは再計算対象なので除外
        var targetIds = new HashSet<TBomKey>(targetNodes.Select(n => n.Id));
        foreach (var refNode in referenceNodes.Where(n => !targetIds.Contains(n.Id)))
        {
            foreach (var step in refNode.ProcessSteps)
                resourceManager.Book<TOperation, TOperationKey>(step, step.LineId, step.MemberId); // 既存リソースIDが取れるなら渡す
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
            item => item.Info.BaseDate
        );

        var completedSteps = new HashSet<TOperation>();

        foreach (var item in allStepsWithMeta)
        {
            var baseLeadTime = item.Step.LeadTime ?? unit;
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

                currentCursor = currentCursor.Add(currentStep.PreBuffer);
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
                currentCursor = currentCursor.Subtract(currentStep.PostBuffer);
            }

            // 計算実行
            var result = useForward
                    ? resourceManager.FindBestSlotForward<TOperation, TOperationKey>(item.Step, currentCursor, baseLeadTime, limit)
                    : resourceManager.FindBestSlotBackward<TOperation, TOperationKey>(item.Step, currentCursor, baseLeadTime, limit);

            // 結果を保存
            item.Step.Start = result.Start;
            item.Step.End = result.End;
            item.Step.WorkingSpan = result.WorkingSpan;
            item.Step.LineId = result.LineId;
            item.Step.MemberId = result.MemberId;

            // 制約違反を記録
            if (result.Violation != ScheduleViolationType.None)
            {
                var constraintDate = result.Violation == ScheduleViolationType.DeadlineMiss
                    ? (item.Info.Mode == BaseDateMode.Deadline ? item.Info.BaseDate : stepCursors[item.Step])
                    : stepCursors[item.Step];
                var actualDate = result.Violation == ScheduleViolationType.DeadlineMiss
                    ? result.End
                    : result.Start;
                violations.Add(new ScheduleViolationInfo(item.Step.Id!, result.Violation, constraintDate, actualDate));
            }

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
            PropagateConstraints<TBom, TBomKey, TOperation, TOperationKey>(item.Node, item.Step, isForward, stepCursors, useForwardForLinks: useForward);

            // リソース予約の確定
            resourceManager.Book<TOperation, TOperationKey>(item.Step, result.LineId, result.MemberId, result.EffectiveSetupTime, result.EffectivePostSetupTime);

            completedSteps.Add(currentStep);
        }

        return violations;
    }

    /// <summary>
    /// 計算済みのステップから、関連するすべてのステップへ時間的制約を波及させる
    /// </summary>
    private static void PropagateConstraints<TBom, TBomKey, TOperation, TOperationKey>(
        TBom currentNode,
        TOperation calculatedStep,
        bool isForward,
        Dictionary<TOperation, DateTime> stepConstraints,
        bool useForwardForLinks = true)
        where TBom : class, IBomNode<TBom, TBomKey, TOperationKey, TOperation>
        where TOperation : IProcessStep<TOperationKey>
        where TBomKey : notnull

    {
        var start = calculatedStep.Start ?? DateTime.MinValue;
        var end = calculatedStep.End ?? DateTime.MaxValue;

        // 1. 同一ノード内の他のステップへの伝播（StepOrder ベースの FS 制約）
        foreach (var other in currentNode.ProcessSteps)
        {
            if (object.ReferenceEquals(other, calculatedStep)) continue;

            if (other.StepOrder < calculatedStep.StepOrder)
            {
                // 先行工程に対して：今回の開始日以前に終わらなければならない (Min)
                var gap = other.PostSetupTime + other.PostBuffer + calculatedStep.PreBuffer + calculatedStep.SetupTime;
                var latestEnd = start - gap;
                if (latestEnd < stepConstraints[other]) stepConstraints[other] = latestEnd;
            }
            else if (other.StepOrder > calculatedStep.StepOrder)
            {
                // 後続工程に対して：今回の終了日以降に始めなければならない (Max)
                var gap = calculatedStep.PostSetupTime + calculatedStep.PostBuffer + other.PreBuffer + other.SetupTime;
                var earliestStart = end + gap;
                if (earliestStart > stepConstraints[other]) stepConstraints[other] = earliestStart;
            }
        }

        // 1b. StepLinks による明示的な依存制約（SS/FF/SF）
#pragma warning disable CS8714
        var stepById = currentNode.ProcessSteps.ToDictionary(s => s.Id!);
#pragma warning restore CS8714
        foreach (var link in currentNode.StepLinks)
        {
            // calculatedStep が From 側の場合：To 側に制約を伝播
            if (link.FromStepId is TOperationKey fromId && EqualityComparer<TOperationKey>.Default.Equals(fromId, calculatedStep.Id))
            {
                if (link.ToStepId is TOperationKey toId && stepById.TryGetValue(toId, out var toStep) && stepConstraints.ContainsKey(toStep))
                {
                    ApplyStepLinkConstraint(link.Type, link.Lag, start, end, toStep, stepConstraints, isForward: useForwardForLinks);
                }
            }
            // calculatedStep が To 側の場合：From 側に制約を伝播（逆向き）
            else if (link.ToStepId is TOperationKey toId2 && EqualityComparer<TOperationKey>.Default.Equals(toId2, calculatedStep.Id))
            {
                if (link.FromStepId is TOperationKey fromId2 && stepById.TryGetValue(fromId2, out var fromStep) && stepConstraints.ContainsKey(fromStep))
                {
                    ApplyStepLinkConstraint(link.Type, link.Lag, start, end, fromStep, stepConstraints, isForward: !useForwardForLinks);
                }
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
                //if (end > stepConstraints[pStep]) stepConstraints[pStep] = end;
                var gap = calculatedStep.PostSetupTime + calculatedStep.PostBuffer + pStep.PreBuffer + pStep.SetupTime;
                var earliestStart = end + gap;
                if (earliestStart > stepConstraints[pStep]) stepConstraints[pStep] = earliestStart;
            }
        }

        // 3. 子ノード（先行ノード）への伝播
        foreach (var childNode in currentNode.Children)
        {
            foreach (var cStep in childNode.ProcessSteps)
            {
                // 子（先行）の全ステップは、このノードの開始日より前に終わらなければならない
                //if (start < stepConstraints[cStep]) stepConstraints[cStep] = start;
                var gap = cStep.PostSetupTime + cStep.PostBuffer + calculatedStep.PreBuffer + calculatedStep.SetupTime;
                var latestEnd = start - gap;
                if (latestEnd < stepConstraints[cStep]) stepConstraints[cStep] = latestEnd;
            }
        }
    }


    /// <summary>
    /// StepLink の種別に応じた制約を対象ステップの stepConstraints に反映する。
    /// </summary>
    private static void ApplyStepLinkConstraint<TStep>(
        StepDependencyType type,
        TimeSpan lag,
        DateTime fromStart,
        DateTime fromEnd,
        TStep targetStep,
        Dictionary<TStep, DateTime> stepConstraints,
        bool isForward)
        where TStep : notnull
    {
        switch (type)
        {
            case StepDependencyType.FinishToStart:
                // To.Start >= From.End + lag
                var fsConstraint = fromEnd + lag;
                if (fsConstraint > stepConstraints[targetStep]) stepConstraints[targetStep] = fsConstraint;
                break;

            case StepDependencyType.StartToStart:
                if (isForward)
                {
                    // To.Start >= From.Start + lag
                    var ssConstraint = fromStart + lag;
                    if (ssConstraint > stepConstraints[targetStep]) stepConstraints[targetStep] = ssConstraint;
                }
                else
                {
                    // From.Start <= To.Start - lag (逆方向：From の締め切りを絞る)
                    var ssConstraint = fromEnd - lag;
                    if (ssConstraint < stepConstraints[targetStep]) stepConstraints[targetStep] = ssConstraint;
                }
                break;

            case StepDependencyType.FinishToFinish:
                if (isForward)
                {
                    // To.End >= From.End + lag → To の着手可能日を From.End + lag - To.LeadTime に相当する値で制約
                    var ffConstraint = fromEnd + lag;
                    if (ffConstraint > stepConstraints[targetStep]) stepConstraints[targetStep] = ffConstraint;
                }
                else
                {
                    // From.End <= To.End - lag (逆方向)
                    var ffConstraint = fromStart - lag;
                    if (ffConstraint < stepConstraints[targetStep]) stepConstraints[targetStep] = ffConstraint;
                }
                break;

            case StepDependencyType.StartToFinish:
                // To.End >= From.Start + lag
                var sfConstraint = fromStart + lag;
                if (sfConstraint > stepConstraints[targetStep]) stepConstraints[targetStep] = sfConstraint;
                break;
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


    /// <summary>
    /// Represents a unique identifier for a resource, consisting of a resource type and an associated identifier.
    /// </summary>
    /// <remarks>A ResourceKey is used to distinguish resources by combining their type with a unique
    /// identifier. This struct is immutable and can be used as a key in collections that require value-based equality,
    /// such as dictionaries or hash sets. Two ResourceKey instances are considered equal if both their Type and Id
    /// values are equal.</remarks>
    public readonly struct ResourceKey : IEquatable<ResourceKey>
    {
        /// <summary>
        /// Gets the type of the resource represented by this instance.
        /// </summary>
        public readonly ResourceType Type;

        /// <summary>
        /// Gets the unique identifier associated with the instance.
        /// </summary>
        public readonly object Id;

        /// <summary>
        /// Initializes a new instance of the ResourceKey class with the specified resource type and identifier.
        /// </summary>
        /// <param name="type">The type of the resource to associate with this key.</param>
        /// <param name="id">The unique identifier for the resource. Must be non-null and uniquely identify the resource within the
        /// specified type.</param>
        public ResourceKey(ResourceType type, object id)
        {
            Type = type;
            Id = id;
        }

        public bool Equals(ResourceKey other) => Type == other.Type && Equals(Id, other.Id);
        public override bool Equals(object? obj) => obj is ResourceKey other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Type, Id);
    }

    /// <summary>
    /// Provides an equality comparer for ResourceKey instances that supports custom comparison logic per resource type.
    /// </summary>
    /// <remarks>This comparer allows callers to specify custom equality comparers for different ResourceType
    /// values via a dictionary. If no custom comparer is provided for a given type, the default object equality
    /// comparer is used. This is useful when different resource types require specialized comparison logic for their
    /// identifiers. The comparer can be used in collections or algorithms that require equality checks or hash codes
    /// for ResourceKey objects.</remarks>
    public class ResourceKeyComparer : IEqualityComparer<ResourceKey>
    {
        private readonly Dictionary<ResourceType, IEqualityComparer<object>> _comparers;
        private readonly IEqualityComparer<object> _defaultComparer = EqualityComparer<object>.Default;

        /// <summary>
        /// Initializes a new instance of the ResourceKeyComparer class using the specified dictionary of comparers for
        /// resource types.
        /// </summary>
        /// <remarks>Use this constructor to provide custom equality comparers for specific resource types
        /// when comparing resource keys. If no comparers are provided, default equality comparison is used for all
        /// resource types.</remarks>
        /// <param name="comparers">A dictionary that maps each ResourceType to its corresponding IEqualityComparer<object>. If null, an empty
        /// dictionary is used.</param>
        public ResourceKeyComparer(Dictionary<ResourceType, IEqualityComparer<object>>? comparers)
        {
            // 外部から渡された比較器の辞書を保持（null なら空の辞書）
            _comparers = comparers ?? new Dictionary<ResourceType, IEqualityComparer<object>>();
        }

        public bool Equals(ResourceKey x, ResourceKey y)
        {
            if (x.Equals(y)) return true;

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

    /// <summary>
    /// Specifies the type of resource to be used or referenced.
    /// </summary>
    /// <remarks>Use this enumeration to indicate whether a resource is associated with a specific line, a
    /// member, or is global in scope. The meaning of each value depends on the context in which the enumeration is
    /// used.</remarks>
    public enum ResourceType
    {
        /// <summary>
        /// Represents a single line of data.
        /// </summary>
        Line,

        /// <summary>
        /// Represents a member
        /// </summary>
        Member
    }


    //製造ラインとメンバーの指定がある場合、その空き状況を管理しておくクラス
    /// <summary>
    /// Manages scheduling and availability of resources, such as production lines and members, for booking and
    /// allocation in manufacturing or similar environments.
    /// </summary>
    /// <remarks>The ResourceScheduler coordinates resource usage by tracking non-working periods and
    /// reservations for both global and resource-specific contexts. It provides methods to register non-working times,
    /// book resources for operations, and search for optimal time slots considering efficiency, setup, and batch
    /// constraints. This class is designed to support forward and backward scheduling scenarios, handling complex
    /// constraints such as overlapping reservations, non-working calendars, and resource capacities. Thread safety is
    /// not guaranteed; external synchronization is required if used concurrently.</remarks>
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
        public void RegisterGlobalNonWorkingTime(IEnumerable<IPendingTimeRange> ranges) => _globalNonWorking.AddRange(ranges);


        /// <summary>
        /// Books the specified process step for the given resource line and member, reserving the associated time range
        /// for resource usage.
        /// </summary>
        /// <typeparam name="TOperation">The type of the process step to be booked. Must implement the IProcessStep interface with the specified
        /// operation key type.</typeparam>
        /// <typeparam name="TOperationKey">The type of the key used to identify the process step.</typeparam>
        /// <param name="step">The process step to book. Must have valid lead time, start, and end values.</param>
        /// <param name="lineId">An optional identifier for the resource line to associate with the booking. Can be null to indicate no
        /// specific line.</param>
        /// <param name="memberId">An optional identifier for the resource member to associate with the booking. Can be null to indicate no
        /// specific member.</param>
        public void Book<TOperation, TOperationKey>(TOperation step, object? lineId, object? memberId, TimeSpan? effectiveSetupTime = null, TimeSpan? effectivePostSetupTime = null)
                    where TOperation : IProcessStep<TOperationKey>
        {
            if (step.LeadTime == null || !step.Start.HasValue || !step.End.HasValue) return;

            var setup = effectiveSetupTime ?? step.SetupTime;
            var postSetup = effectivePostSetupTime ?? step.PostSetupTime;

            var range = new TimeRangeInstance
            {
                Start = step.Start - setup,
                End = step.End + postSetup
            };
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
        /// Finds the earliest available time slot for the specified process step, starting from the given cursor, by
        /// evaluating possible resource combinations and their efficiencies.
        /// </summary>
        /// <remarks>This method considers all combinations of available lines and members, adjusting the
        /// required duration based on their combined efficiencies. If no candidates are found, the method falls back to
        /// scheduling with default efficiency. The returned ScheduleResult includes the scheduled start and end times,
        /// resource identifiers, and the actual duration.</remarks>
        /// <typeparam name="TOperation">The type of the process step to be scheduled. Must implement the IProcessStep interface.</typeparam>
        /// <typeparam name="TOperationKey">The type of the key used to identify the process step.</typeparam>
        /// <param name="step">The process step for which to find an available time slot. Cannot be null.</param>
        /// <param name="startCursor">The date and time from which to begin searching for an available slot.</param>
        /// <param name="baseDuration">The base duration required to complete the process step, before efficiency adjustments are applied.</param>
        /// <param name="latestFinish">The latest allowable finish time for the scheduled step, or null if there is no constraint.</param>
        /// <returns>A ScheduleResult representing the best available slot found for the process step. If no suitable resources
        /// are available, returns a result based on default efficiency and the provided start time.</returns>
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
                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, totalEfficiency);

                    // 段取り時間に効率を適用するかどうかに応じて実効段取り時間を算出
                    var effectiveSetup = step.ScaleSetupByEfficiency
                        ? TimeSpan.FromTicks((long)(step.SetupTime.Ticks / Math.Max(0.01, totalEfficiency)))
                        : step.SetupTime;
                    var effectivePostSetup = step.ScaleSetupByEfficiency
                        ? TimeSpan.FromTicks((long)(step.PostSetupTime.Ticks / Math.Max(0.01, totalEfficiency)))
                        : step.PostSetupTime;

                    // リソースを占有すべき全時間を計算 (Setup + Lead + PostSetup)
                    var totalOccupancy = effectiveSetup + actualDuration + effectivePostSetup;

                    var (foundStart, foundEnd, violation) = FindFirstAvailableStart(keys, startCursor, totalOccupancy, step.BatchCapacity, latestFinish);


                    var leadStart = foundStart.Add(effectiveSetup);
                    var leadEnd = foundEnd.Subtract(effectivePostSetup);

                    // 「最も早く終わる」組み合わせを保持
                    if (bestResult == null || leadEnd < bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(leadStart, leadEnd, line.Id, member.Id, actualDuration, violation, effectiveSetup, effectivePostSetup);
                    }
                }

            // 候補がない場合のフォールバック（効率1.0で計算）
            return bestResult ?? new ScheduleResult(startCursor, startCursor.Add(CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, 1.0)), null, null, baseDuration);
        }

        /// <summary>
        /// Finds the optimal time slot for scheduling the specified process step by searching backward from the given
        /// end time, considering resource availability and efficiency constraints.
        /// </summary>
        /// <remarks>This method evaluates combinations of available lines and members to maximize
        /// scheduling efficiency. If no candidates are available, a fallback slot is returned using the default
        /// efficiency. The method does not guarantee that the returned slot is feasible if resource constraints change
        /// after the call.</remarks>
        /// <typeparam name="TOperation">The type of process step to schedule. Must implement the IProcessStep interface.</typeparam>
        /// <typeparam name="TOperationKey">The type of the key used to identify the process step.</typeparam>
        /// <param name="step">The process step to be scheduled. Must not be null.</param>
        /// <param name="endCursor">The latest possible end time for the scheduled slot. The search will proceed backward from this point.</param>
        /// <param name="baseDuration">The base duration required to complete the process step, before efficiency adjustments are applied.</param>
        /// <param name="earliestStart">The earliest allowed start time for the scheduled slot. If null, no lower bound is enforced.</param>
        /// <returns>A ScheduleResult representing the best available time slot found for the process step. If no suitable slot
        /// is found, returns a fallback result using the default efficiency.</returns>
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
                    var keys = GetSelectedResourceKeys(line.Id, member.Id).ToList();

                    // 効率を加味した実際の所要時間を計算
                    var actualDuration = CalculateActualDuration<TOperation, TOperationKey>(step, baseDuration, totalEfficiency);

                    // 段取り時間に効率を適用するかどうかに応じて実効段取り時間を算出
                    var effectiveSetup = step.ScaleSetupByEfficiency
                        ? TimeSpan.FromTicks((long)(step.SetupTime.Ticks / Math.Max(0.01, totalEfficiency)))
                        : step.SetupTime;
                    var effectivePostSetup = step.ScaleSetupByEfficiency
                        ? TimeSpan.FromTicks((long)(step.PostSetupTime.Ticks / Math.Max(0.01, totalEfficiency)))
                        : step.PostSetupTime;

                    // リソースを占有すべき全時間を計算 (Setup + Lead + PostSetup)
                    var totalOccupancy = effectiveSetup + actualDuration + effectivePostSetup;

                    // 2. 探索。endCursorから遡って searchDuration 分の空きを探す
                    var (foundStart, foundEnd, violation) = FindFirstAvailableEnd(keys, endCursor, totalOccupancy, step.BatchCapacity, earliestStart);

                    // 3. スケジュール結果の算出
                    // LeadTimeの終了 = 見つかった枠の末尾 - PostSetupTime
                    var leadEnd = foundEnd.Subtract(effectivePostSetup);
                    var leadStart = foundStart.Subtract(effectiveSetup);

                    if (bestResult == null || leadEnd > bestResult.Value.End)
                    {
                        bestResult = new ScheduleResult(leadStart, leadEnd, line.Id, member.Id, actualDuration, violation, effectiveSetup, effectivePostSetup);
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


        /// <summary>
        /// Finds the earliest available time window that satisfies the specified duration and capacity constraints
        /// within the working calendar for the given resources.
        /// </summary>
        /// <remarks>If no suitable window is found before the latest finish time, the method returns a
        /// window ending at the latest finish and starting as late as possible to accommodate the required duration.
        /// The search skips non-working periods and avoids conflicts with existing reservations, except for calendar
        /// holidays, which are automatically excluded from the search.</remarks>
        /// <param name="keys">A list of resource keys representing the resources to check for availability.</param>
        /// <param name="start">The earliest date and time at which the search for an available window should begin.</param>
        /// <param name="duration">The required length of the available window as a time span. Only working time is counted toward this
        /// duration.</param>
        /// <param name="capacity">The minimum required capacity for the resources during the scheduled window.</param>
        /// <param name="latestFinish">An optional latest finish time. If specified, the returned window will not end after this time.</param>
        /// <returns>A tuple containing the start and end date-times of the first available window that meets the specified
        /// criteria. The window is guaranteed to fall within working hours and not exceed the latest finish time if
        /// provided.</returns>
        private (DateTime Start, DateTime End, ScheduleViolationType Violation) FindFirstAvailableStart(List<ResourceKey> keys, DateTime start, TimeSpan duration, double capacity, DateTime? latestFinish)
        {
            // 1. まず開始地点を稼働時間（非稼働じゃない時間）まで進める
            var currentStart = MoveToAvailableSlot(keys, start, true);

            while (true)
            {
                // 2. 稼働時間だけをカウントして終了候補日を計算
                var endCandidate = AddWorkingTime(keys, currentStart, duration);

                if (latestFinish.HasValue && endCandidate > latestFinish.Value)
                {
                    return (latestFinish.Value.Subtract(duration), latestFinish.Value, ScheduleViolationType.DeadlineMiss);
                }
                // 3. 衝突を確認
                var collision = FindCollisionWithBatch(keys, currentStart, endCandidate, capacity);

                if (collision == null || IsCalendarHoliday(keys, collision))
                {
                    // 衝突がない、またはカレンダーの休みの場合は、この開始・終了ペアが正解
                    return (currentStart, endCandidate, ScheduleViolationType.None);
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
        private (DateTime Start, DateTime End, ScheduleViolationType Violation) FindFirstAvailableEnd(List<ResourceKey> keys, DateTime end, TimeSpan duration, double capacity, DateTime? earliestStart)
        {
            var currentEnd = MoveToAvailableSlot(keys, end, false);
            while (true)
            {
                var startCandidate = SubtractWorkingTime(keys, currentEnd, duration);

                if (earliestStart.HasValue && startCandidate < earliestStart.Value)
                {
                    return (earliestStart.Value, earliestStart.Value.Add(duration), ScheduleViolationType.EarliestStartViolation);
                }
                var collision = FindCollisionWithBatch(keys, startCandidate, currentEnd, capacity);

                if (collision == null || IsCalendarHoliday(keys, collision))
                {
                    // 衝突がない、またはカレンダー由来の休みの場合は、このペアを返す
                    return (startCandidate, currentEnd, ScheduleViolationType.None);
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

        /// <summary>
        /// Searches for the first pending time range that overlaps with the specified time interval for the given
        /// resources.
        /// </summary>
        /// <remarks>The method checks for collisions in the following order: global non-working periods,
        /// resource-specific non-working periods, and existing bookings for each resource. The search stops at the
        /// first detected collision.</remarks>
        /// <param name="resourceKeys">A collection of resource keys to check for collisions with the specified time interval.</param>
        /// <param name="start">The start date and time of the interval to check for collisions.</param>
        /// <param name="end">The end date and time of the interval to check for collisions.</param>
        /// <returns>An object representing the first pending time range that collides with the specified interval, or null if no
        /// collision is found.</returns>
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


        /// <summary>
        /// 見つかった衝突が「カレンダー由来」か「他人の予約由来」かを判定する補助関数
        /// </summary>
        private bool IsCalendarHoliday(List<ResourceKey> keys, IPendingTimeRange collision)
        {
            if (collision == null) return false;

            // この衝突オブジェクト自体が、リソース予約（usages）の中に存在するか確認する
            // もし予約データなら、それは「純粋な休日」ではない
            foreach (var key in keys)
            {
                if (_resourceUsage.TryGetValue(key, out var usages) && usages.Contains(collision))
                {
                    return false;
                }
            }

            // 予約リストに存在しないなら、それはグローバルまたは個別の非稼働設定である
            return true;
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
            // --- 1. まず「予約（リソース使用状況）」との衝突を最優先でチェック ---
            foreach (var key in keys)
            {
                if (_resourceUsage.TryGetValue(key, out var usages))
                {
                    var overlaps = usages.Where(u => IsOverlap(u, start, end)).ToList();
                    if (overlaps.Count > 0)
                    {
                        if (capacity <= 1)
                        {
                            // 排他リソース：1件でも重なれば衝突
                            return overlaps[0];
                        }

                        // バッチリソース：時系列スイープで最大同時稼働数を算出
                        // （A=10-11, B=11-12 の2件は count=2 だが同時には1件しか稼働しない）
                        int maxConcurrent = 0;
                        var cursor = start;
                        while (cursor < end)
                        {
                            var sliceEnd = cursor.Add(_unit);
                            var concurrentCount = overlaps.Count(u => IsOverlap(u, cursor, sliceEnd));
                            if (concurrentCount > maxConcurrent) maxConcurrent = concurrentCount;
                            cursor = sliceEnd;
                        }

                        if (maxConcurrent >= capacity)
                        {
                            return overlaps[0];
                        }
                    }
                }
            }

            // --- 2. 予約がない場合のみ、「非稼働（休日）」をチェック ---
            var globalHit = _globalNonWorking.FirstOrDefault(nw => IsOverlap(nw, start, end));
            if (globalHit != null) return globalHit;

            foreach (var key in keys)
            {
                var calHit = _resNonWorkingGetter?.Invoke(key).FirstOrDefault(nw => IsOverlap(nw, start, end));
                if (calHit != null) return calHit;
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

        private class TimeRangeInstance : IPendingTimeRange { public DateTime? Start { get; set; } public DateTime? End { get; set; } public TimeSpan? WorkingSpan { get; set; } }

        /// <summary>
        /// Represents the result of a scheduling operation, including the scheduled time range, associated line and
        /// member identifiers, and the total working duration.
        /// </summary>
        /// <remarks>This structure is typically used to convey the outcome of a scheduling process, such
        /// as shift assignments or resource allocations. The identifiers for line and member are stored as objects to
        /// allow for flexibility in identifier types. All fields are publicly accessible for direct use in scheduling
        /// logic or reporting.</remarks>
        public struct ScheduleResult
        {
            public DateTime Start;
            public DateTime End;
            public object? LineId;
            public object? MemberId;
            public TimeSpan WorkingSpan;
            public ScheduleViolationType Violation;
            public TimeSpan EffectiveSetupTime;
            public TimeSpan EffectivePostSetupTime;

            public ScheduleResult(DateTime s, DateTime e, object? l, object? m, TimeSpan workingSpan, ScheduleViolationType violation = ScheduleViolationType.None, TimeSpan effectiveSetup = default, TimeSpan effectivePostSetup = default)
            {
                Start = s; End = e; LineId = l; MemberId = m; WorkingSpan = workingSpan; Violation = violation;
                EffectiveSetupTime = effectiveSetup; EffectivePostSetupTime = effectivePostSetup;
            }
        }
    }
}


