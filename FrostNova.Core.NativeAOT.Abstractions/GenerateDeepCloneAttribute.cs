using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">DeepClone メソッドとコピーロジックを自動生成することを指定します。</para>
    /// <para xml:lang="en">Specifies that the DeepClone method and cloning logic should be automatically generated.</para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="ja">クラスに付与すると IDeepCloneable インターフェースを実装し、部分メソッドに付与するとそのメソッドの実装を生成します。</para>
    /// <para xml:lang="en">When applied to a class, it implements the IDeepCloneable interface. When applied to a partial method, it generates the implementation for that method.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class GenerateDeepCloneAttribute : Attribute
    {
    }
}
