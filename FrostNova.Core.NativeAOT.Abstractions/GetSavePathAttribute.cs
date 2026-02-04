using System;
using System.Collections.Generic;
using System.Text;

namespace FrostNova.Core.NativeAOT
{
    /// <summary>
    /// <para xml:lang="ja">設定ファイルの保存先パスを返すメソッドを指定します。</para>
    /// <para xml:lang="en">Specifies the method that returns the save path for the settings file.</para>
    /// </summary>
    /// <remarks>
    /// <para xml:lang="ja">この属性は、戻り値が string 型で引数を持たない static メソッドに付与してください。LoadSettings/SaveSettings メソッドの生成に必要です。</para>
    /// <para xml:lang="en">This attribute should be applied to a static method that returns a string and takes no arguments. It is required to generate LoadSettings/SaveSettings methods.</para>
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public class GetSavePathAttribute  : Attribute
    {
    }
}
