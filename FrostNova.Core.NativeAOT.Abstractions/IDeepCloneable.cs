using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">オブジェクトの深い複製（Deep Clone）をサポートしていることを示します。</para>
    /// <para xml:lang="en">Indicates that the object supports deep cloning.</para>
    /// </summary>
    /// <typeparam name="T">
    /// <para xml:lang="ja">複製されるオブジェクトの型。 / The type of the object to be cloned.</para>
    /// </typeparam>
    public interface IDeepCloneable<T>
    {
        /// <summary>
        /// <para xml:lang="ja">このインスタンスの内容を別のインスタンスへコピーします。</para>
        /// <para xml:lang="en">Copies the content of this instance into another instance.</para>
        /// </summary>
        /// <param name="target">
        /// <para xml:lang="ja">コピー先のインスタンス。 / The target instance to copy values to.</para>
        /// </param>
        void CopyTo(T target);

        /// <summary>
        /// <para xml:lang="ja">このインスタンスのDeep cloneを作成します。</para>
        /// <para xml:lang="en">Creates a deep clone of this instance.</para>
        /// </summary>
        /// <returns>
        /// <para xml:lang="ja">複製された新しいインスタンス。 / A new instance that is a deep copy of this instance.</para>
        /// </returns>
        T DeepClone();
    }
}
