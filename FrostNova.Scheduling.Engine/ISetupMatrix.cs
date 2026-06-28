namespace FrostNova.Scheduling.Engine;

/// <summary>
/// Provides dynamic setup times based on the combination of the previous and next item.
/// Typical scenarios: no setup required for the same item in sequence; cleaning or fixture change
/// required when switching to a different item.
/// </summary>
public interface ISetupMatrix
{
    /// <summary>
    /// Returns the setup time on a line when transitioning from <paramref name="fromItemKey"/> to
    /// <paramref name="toItemKey"/>. A <see langword="null"/> <paramref name="fromItemKey"/> means
    /// this is the first item scheduled on the line (no predecessor).
    /// </summary>
    TimeSpan GetSetupTime(object? fromItemKey, object? toItemKey);
}

/// <summary>
/// Type-safe generic extension of <see cref="ISetupMatrix"/>. Implement this when item keys are
/// strongly typed as <typeparamref name="TKey"/>.
/// </summary>
public interface ISetupMatrix<TKey> : ISetupMatrix
{
    /// <summary>Type-safe variant of <see cref="ISetupMatrix.GetSetupTime"/>.</summary>
    TimeSpan GetSetupTime(TKey? fromItemKey, TKey? toItemKey);

    TimeSpan ISetupMatrix.GetSetupTime(object? fromItemKey, object? toItemKey)
        => GetSetupTime(
            fromItemKey is TKey f ? f : default,
            toItemKey is TKey t ? t : default);
}
