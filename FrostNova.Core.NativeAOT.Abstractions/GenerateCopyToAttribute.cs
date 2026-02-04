using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">既存のインスタンスに値をコピーする実装を自動生成します。</para>
    /// <para xml:lang="en">Generates an implementation that copies values into an existing instance.</para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="ja">この属性は partial メソッド（例: static partial void CopyTo(T source, T target)）に付与してください。</para>
    /// <para xml:lang="en">This attribute should be applied to a partial method (e.g., static partial void CopyTo(T source, T target)).</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class GenerateCopyPropertyAttribute : Attribute
    {
    }
}
