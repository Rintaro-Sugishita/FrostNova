using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">JSON 形式による設定の読み書きメソッドを自動生成することを指定します。</para>
    /// <para xml:lang="en">Specifies that methods for reading and writing settings in JSON format should be automatically generated.</para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="ja">この属性を付与すると、NativeAOT に対応した JsonSerializerContext が内部で生成され、LoadFromFile や SaveToFile などのメソッドが追加されます。</para>
    /// <para xml:lang="en">When applied, a NativeAOT-compatible JsonSerializerContext is generated internally, and methods such as LoadFromFile and SaveToFile are added.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public class GenerateJsonSettingsAttribute : Attribute
    {
        /// <summary>
        /// <para xml:lang="ja">使用する JsonSerializerContext の型を指定します。</para>
        /// <para xml:lang="en">Specifies the type of JsonSerializerContext to use.</para>
        /// </summary>
        public Type? ContextType { get; }

        /// <summary>
        /// <para xml:lang="ja">JSON 形式による設定の読み書きメソッドを自動生成することを指定します。</para>
        /// <para xml:lang="en">Specifies that methods for reading and writing settings in JSON format should be automatically generated.</para>
        /// </summary>
        public GenerateJsonSettingsAttribute(Type? contextType = null) => ContextType = contextType;
    }
}
