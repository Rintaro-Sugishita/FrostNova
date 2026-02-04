using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">このプロパティを複製処理の対象から除外します。</para>
    /// <para xml:lang="en">Excludes this property from the cloning process.</para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="ja">循環参照を回避する場合や、特定のプロパティをコピーしたくない場合に使用します。</para>
    /// <para xml:lang="en">Use this to prevent circular references or when you want to skip copying specific properties.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class DeepCloneIgnoreAttribute : Attribute
    {
    }
}
