namespace FrostNova.Scheduling.Engine;

/// <summary>
/// 直前の品番と次の品番の組み合わせに応じた動的な段取り時間を提供するインターフェース。
/// 同品番連続なら段取り不要、異品番は洗浄・治具交換が必要といったシナリオで使用する。
/// </summary>
public interface ISetupMatrix
{
    /// <summary>
    /// 指定したラインで直前に処理した品番（from）から次の品番（to）への段取り時間を返す。
    /// fromItemKey が null の場合はラインの初回投入（前品番なし）を意味する。
    /// </summary>
    TimeSpan GetSetupTime(object? fromItemKey, object? toItemKey);
}

/// <summary>
/// 型安全な段取りマトリクスのジェネリック拡張インターフェース。
/// TKey 型のキーで品番を扱いたい場合はこちらを実装する。
/// </summary>
public interface ISetupMatrix<TKey> : ISetupMatrix
{
    /// <summary>型安全な段取り時間取得。</summary>
    TimeSpan GetSetupTime(TKey? fromItemKey, TKey? toItemKey);

    TimeSpan ISetupMatrix.GetSetupTime(object? fromItemKey, object? toItemKey)
        => GetSetupTime(
            fromItemKey is TKey f ? f : default,
            toItemKey is TKey t ? t : default);
}
