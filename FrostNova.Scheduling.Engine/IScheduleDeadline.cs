namespace FrostNova.Scheduling.Engine;

/// <summary>
/// 希望納期・絶対納期・在庫許容限界を BOM ノードに付与するオプションインターフェース。
/// IBomNode の実装クラスがこのインターフェースを実装することで、
/// SimulationEngine によるスコア評価の対象となる。
/// </summary>
public interface IScheduleDeadline
{
    /// <summary>希望納期。ここからのズレが評価スコアのペナルティになる。</summary>
    DateTime TargetDate { get; }

    /// <summary>絶対納期。最終工程の終了がこの日時を超えたシナリオはハード違反として評価から除外される。</summary>
    DateTime Deadline { get; }

    /// <summary>
    /// 在庫許容限界（前倒し禁止日）。
    /// いずれかの工程の開始がこの日時より前になると在庫ペナルティが発生する。
    /// 倉庫キャパシティやキャッシュフローの制約を表す。
    /// </summary>
    DateTime EarliestStart { get; }

    /// <summary>
    /// 製品・顧客の重要度係数（デフォルト 1.0）。
    /// 希望納期ズレのペナルティにこの係数を乗算する。
    /// 例: 2.0 なら同じ1時間遅れでも重要度1.0の製品の2倍のペナルティになる。
    /// </summary>
    double ImportanceWeight { get; }

    /// <summary>
    /// LeadTime のリスクバッファ係数（デフォルト 0.0）。
    /// スケジュール計算時に LeadTime を (1 + RiskBuffer) 倍して余裕を持たせる。
    /// 例: 0.2 なら2時間作業を2.4時間として計算し、実作業後に余裕ができる。
    /// </summary>
    double RiskBuffer { get; }
}
